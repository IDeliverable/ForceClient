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

namespace IDeliverable.ForceClient.Metadata.Client
{
    public class SoapMetadataClient : IMetadataClient
    {
        // TODO: Introduce an IAccessTokenProvider and implementation.

        public SoapMetadataClient(string url, string accessToken, MetadataRules metadataRules, ILogger<SoapMetadataClient> logger)
        {
            mUrl = new Uri(url.Replace("{version}", metadataRules.MetadataApiVersion.ToString()));
            mAccessToken = accessToken;
            mMetadataRules = metadataRules;
            mLogger = logger;

            mClient = new MetadataPortTypeClient();
            mClient.Endpoint.Address = new EndpointAddress(mUrl);
            mSessionHeader = new SessionHeader() { sessionId = mAccessToken };
            mMapper = new MapperConfiguration(cfg => { }).CreateMapper();
            mRetryPolicy = Policy.Handle<TimeoutException>().WaitAndRetryAsync(3, x => TimeSpan.FromSeconds(3));
        }

        private readonly Uri mUrl;
        private readonly string mAccessToken;
        private readonly MetadataRules mMetadataRules;
        private readonly ILogger mLogger;
        private readonly MetadataPortTypeClient mClient;
        private readonly SessionHeader mSessionHeader;
        private readonly IMapper mMapper;
        private readonly Policy mRetryPolicy;

        public async Task<IEnumerable<MetadataFolderInfo>> ListFoldersAsync(IEnumerable<MetadataType> types)
        {
            var folderSuffix = "Folder";

            if (types.Count() > mMetadataRules.MaxListMetadataQueriesPerRequest)
                throw new ArgumentOutOfRangeException(nameof(types), $"The number of metadata types ({types.Count()}) is greater than the maximum number allowed per request ({mMetadataRules.MaxListMetadataQueriesPerRequest}).");

            if (types.Any(type => !mMetadataRules.GetIsFolderized(type)))
                throw new ArgumentException("One or more of the specified metadata types is not a folderized type.", nameof(types));

            var folderQueries =
                types
                    .Select(type => new ListMetadataQuery() { type = $"{type}{folderSuffix}" })
                    .ToArray();

            try
            {
                var response = await mRetryPolicy.ExecuteAsync(() => mClient.listMetadataAsync(mSessionHeader, null, folderQueries, mMetadataRules.MetadataApiVersion));

                var folderInfoQuery =
                    from fileProperties in response.result
                    let typeString = fileProperties.type.EndsWith(folderSuffix) ? fileProperties.type.Substring(0, fileProperties.type.Length - folderSuffix.Length) : fileProperties.type
                    let type = (MetadataType)Enum.Parse(typeof(MetadataType), typeString)
                    select new MetadataFolderInfo(fileProperties.fullName, type);
                var folderInfoList = folderInfoQuery.ToArray();

                return folderInfoList;
            }
            catch (Exception ex)
            {
                mLogger.LogError(ex, "Error while listing metadata folders.");
                if (ex is FaultException fex)
                    throw new MetadataException(fex.Reason.ToString(), fex.Code.Name, fex);
                throw;
            }
        }

        public async Task<IEnumerable<MetadataItemInfo>> ListItemsAsync(IEnumerable<MetadataListQuery> queries)
        {
            if (queries.Count() > mMetadataRules.MaxListMetadataQueriesPerRequest)
                throw new ArgumentOutOfRangeException(nameof(queries), $"The number of metadata queries ({queries.Count()}) is greater than the maximum number allowed per request ({mMetadataRules.MaxListMetadataQueriesPerRequest}).");

            if (queries.Any(query => mMetadataRules.GetIsFolderized(query.Type) && String.IsNullOrEmpty(query.InFolder)))
                throw new ArgumentException($"One or more of the specified metadata types is a folderized type but no folder is specified; use {nameof(ListFoldersAsync)} first to obtain the available folders.", nameof(queries));

            var itemsQueries =
                queries
                    .Select(query => new ListMetadataQuery() { type = query.Type.ToString(), folder = query.InFolder })
                    .ToArray();

            try
            {
                var response = await mRetryPolicy.ExecuteAsync(() => mClient.listMetadataAsync(mSessionHeader, null, itemsQueries, mMetadataRules.MetadataApiVersion));

                if (response?.result != null) // Result can sometimes be null for empty folders.
                {
                    var itemInfoQuery =
                        from fileProperties in response.result
                        where !String.IsNullOrEmpty(fileProperties.type)
                        select new MetadataItemInfo((MetadataType)Enum.Parse(typeof(MetadataType), fileProperties.type), fileProperties.fullName);
                    var itemInfoList = itemInfoQuery.ToArray();

                    return itemInfoList;

                    //if (!String.IsNullOrEmpty(itemQuery.folder))
                    //    mLogger.LogInformation($"Found {itemReferenceList.Length} items of type {itemQuery.type} in folder {itemQuery.folder}.");
                    //else
                    //    mLogger.LogInformation($"Found {itemReferenceList.Length} items of type {itemQuery.type}.");
                }

                return new MetadataItemInfo[] { };
            }
            catch (Exception ex)
            {
                //mLogger.LogError(ex, "Error while listing metadata items.");
                if (ex is FaultException fex)
                    throw new MetadataException(fex.Reason.ToString(), fex.Code.Name, fex);
                throw;
            }
        }

        public async Task<string> StartRetrieveAsync(IEnumerable<MetadataItemInfo> items)
        {
            if (items.Count() > mMetadataRules.MaxRetrieveMetadataItemsPerRequest)
                throw new ArgumentOutOfRangeException(nameof(items), $"The number of metadata items to retrieve ({items.Count()}) is greater than the maximum number allowed per request ({mMetadataRules.MaxRetrieveMetadataItemsPerRequest}).");

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
