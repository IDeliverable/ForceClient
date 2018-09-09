using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using AutoMapper;
using IDeliverable.ForceClient.Metadata.Deploy;
using IDeliverable.ForceClient.Metadata.ForceMetadata;
using IDeliverable.ForceClient.Metadata.Retrieve;
using Microsoft.Extensions.Logging;
using Polly;

namespace IDeliverable.ForceClient.Metadata
{
    public class MetadataGateway
    {
        public const int MetadataApiVersion = 43;
        public const int MaxListMetadataQueriesPerRequest = 3;
        public const int MaxRetrieveMetadataItemsPerRequest = 10000;

        public MetadataGateway(string url, string accessToken, ILogger<MetadataGateway> logger)
        {
            mUrl = new Uri(url.Replace("{version}", MetadataApiVersion.ToString()));
            mAccessToken = accessToken;
            mClient = new MetadataPortTypeClient();
            mClient.Endpoint.Address = new EndpointAddress(mUrl);
            mSessionHeader = new SessionHeader() { sessionId = mAccessToken };
            mMapper = new MapperConfiguration(cfg => { }).CreateMapper();
            mLogger = logger;
            mRetryPolicy = Policy.Handle<TimeoutException>().WaitAndRetryAsync(3, x => TimeSpan.FromSeconds(3));
        }

        private readonly Uri mUrl;
        private readonly string mAccessToken;
        private readonly MetadataPortTypeClient mClient;
        private readonly SessionHeader mSessionHeader;
        private readonly IMapper mMapper;
        private readonly ILogger mLogger;
        private readonly Policy mRetryPolicy;

        public IEnumerable<MetadataType> GetAllMetadataTypes()
        {
            return Enum.GetValues(typeof(MetadataType)).Cast<MetadataType>().ToArray();
        }

        public async Task<IEnumerable<MetadataItemReference>> ListItemsAsync(IEnumerable<MetadataType> types)
        {
            var itemQueries = new List<ListMetadataQuery>();

            foreach (var metadataType in types)
            {
                if (metadataType.GetIsInFolders())
                {
                    // For metadata items that live in folders, we need to first make separate API calls
                    // to list each folder type. Once we know the full folder names we can ask the API to
                    // list the items in each folder.
                    var folderQuery = new ListMetadataQuery() { type = $"{metadataType}Folder" };
                    var folderResponse = await mRetryPolicy.ExecuteAsync(() => mClient.listMetadataAsync(mSessionHeader, null, new[] { folderQuery }, MetadataApiVersion));
                    foreach (var fileProperties in folderResponse.result)
                        itemQueries.Add(new ListMetadataQuery() { type = metadataType.ToString(), folder = fileProperties.fullName });
                }
                else
                    itemQueries.Add(new ListMetadataQuery() { type = metadataType.ToString() });
            }

            var result = new List<MetadataItemReference>();

            foreach (var itemQuery in itemQueries)
            {
                listMetadataResponse response = null;

                try
                {
                    response = await mRetryPolicy.ExecuteAsync(() => mClient.listMetadataAsync(mSessionHeader, null, new[] { itemQuery }, MetadataApiVersion));
                }
                catch (FaultException ex) when (ex.Code.Name == "INVALID_TYPE")
                {
                    mLogger.LogWarning(ex.Message);
                }
                catch (Exception ex)
                {
                    mLogger.LogError(ex, $"Error while listing metadata items of type {itemQuery.type}.");
                }

                if (response?.result != null) // Result can sometimes be null for empty folders.
                {
                    var itemReferenceQuery =
                        from fileProperties in response.result
                        where !String.IsNullOrEmpty(fileProperties.type)
                        select new MetadataItemReference((MetadataType)Enum.Parse(typeof(MetadataType), fileProperties.type), fileProperties.fullName);
                    var itemReferenceList = itemReferenceQuery.ToArray();

                    result.AddRange(itemReferenceList);

                    if (!String.IsNullOrEmpty(itemQuery.folder))
                        mLogger.LogInformation($"Found {itemReferenceList.Length} items of type {itemQuery.type} in folder {itemQuery.folder}.");
                    else
                        mLogger.LogInformation($"Found {itemReferenceList.Length} items of type {itemQuery.type}.");
                }
            }

            return result.ToArray();
        }

        public async Task<string> StartRetrieveAsync(IEnumerable<MetadataItemReference> items)
        {
            if (items.Count() > MaxRetrieveMetadataItemsPerRequest)
                throw new ArgumentOutOfRangeException(nameof(items), $"The number of metadata items to retrieve ({items.Count()} is greater than the maximum number allowed {MaxRetrieveMetadataItemsPerRequest}.");

            var typeMembersQuery =
                from itemReference in items
                group itemReference.FullName by itemReference.Type into itemTypeGroup
                select new PackageTypeMembers() { name = itemTypeGroup.Key.ToString(), members = itemTypeGroup.ToArray() };

            var package = new Package() { types = typeMembersQuery.ToArray() };
            var request = new RetrieveRequest() { unpackaged = package };

            var response = await mRetryPolicy.ExecuteAsync(() => mClient.retrieveAsync(mSessionHeader, null, request));

            return response.result.id;
        }

        public async Task<Retrieve.RetrieveResult> GetRetrieveResultAsync(string operationId)
        {
            var response = await mRetryPolicy.ExecuteAsync(() => mClient.checkRetrieveStatusAsync(mSessionHeader, null, operationId, includeZip: true));
            var result = response.result;

            if (result.status == ForceMetadata.RetrieveStatus.Failed)
                throw new RetrieveException(result.errorMessage, result.errorStatusCode.ToString());

            return new Retrieve.RetrieveResult(
                mMapper.Map<ForceMetadata.RetrieveStatus, Retrieve.RetrieveStatus>(result.status),
                result.zipFile);
        }

        public async Task<string> StartDeployAsync(byte[] zipFile)
        {
            // TODO: Expose DeployOptions to API clients.
            var options = new DeployOptions() { checkOnly = false, rollbackOnError = true };
            var response = await mRetryPolicy.ExecuteAsync(() => mClient.deployAsync(mSessionHeader, null, null, zipFile, options));

            return response.result.id;
        }

        public async Task<Deploy.DeployResult> GetDeployResultAsync(string operationId)
        {
            var response = await mRetryPolicy.ExecuteAsync(() => mClient.checkDeployStatusAsync(mSessionHeader, null, operationId, includeDetails: true));
            var result = response.result;

            if (result.status == ForceMetadata.DeployStatus.Canceled)
                throw new DeployCanceledException(result.canceledByName);
            if (result.status == ForceMetadata.DeployStatus.Failed)
                throw new DeployException(result.errorMessage, result.errorStatusCode.ToString());

            return new Deploy.DeployResult(
                mMapper.Map<ForceMetadata.DeployStatus, Deploy.DeployStatus>(result.status),
                result.stateDetail,
                result.numberComponentsTotal,
                result.numberComponentsDeployed,
                result.numberComponentErrors,
                result.numberTestsTotal,
                result.numberTestsCompleted,
                result.numberTestErrors);
        }
    }
}
