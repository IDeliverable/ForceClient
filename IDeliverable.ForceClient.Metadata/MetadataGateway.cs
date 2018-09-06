using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using AutoMapper;
using IDeliverable.ForceClient.Core;
using IDeliverable.ForceClient.Metadata.ForceMetadata;
using IDeliverable.Utils.Core.CollectionExtensions;

namespace IDeliverable.ForceClient.Metadata
{
    public class MetadataGateway
    {
        public const int MetadataApiVersion = 38;
        public const int MaxListMetadataQueriesPerRequest = 3;
        public const int MaxRetrieveMetadataItemsPerRequest = 10000;

        public MetadataGateway(Uri instanceUrl, string sessionId)
        {
            mInstanceUrl = instanceUrl;
            mSessionId = sessionId;

            var mapperConfig =
                new MapperConfiguration(
                    cfg =>
                    {
                    });

            mMapper = mapperConfig.CreateMapper();
        }

        private Uri mInstanceUrl;
        private readonly string mSessionId;
        private IMapper mMapper;

        public async Task<IEnumerable<MetadataItemReference>> ListItemsAsync(IEnumerable<MetadataType> types)
        {
            var itemQueries = new List<ListMetadataQuery>();

            var client = CreateClient();

            foreach (var metadataType in types)
            {
                if (metadataType.GetIsInFolders())
                {
                    // For metadata items that live in folders, we need to first make separate API calls
                    // to list each folder type. Once we know the full folder names we can ask the API to
                    // list the items in each folder.
                    var folderQuery = new ListMetadataQuery() { type = $"{metadataType}Folder" };
                    var folderResponse = await client.listMetadataAsync(null, null, new[] { folderQuery }, MetadataApiVersion);
                    foreach (var fileProperties in folderResponse.result)
                        itemQueries.Add(new ListMetadataQuery() { type = metadataType.ToString(), folder = fileProperties.fullName });
                }
                else
                    itemQueries.Add(new ListMetadataQuery() { type = metadataType.ToString() });
            }

            var listMetadataTasks =
                itemQueries
                    .Partition(MaxListMetadataQueriesPerRequest)
                    .Select(async (itemQueryRange) =>
                    {
                        var response = await client.listMetadataAsync(null, null, itemQueryRange.ToArray(), MetadataApiVersion);
                        var itemsQuery =
                            from fileProperties in response.result
                            select new MetadataItemReference((MetadataType)Enum.Parse(typeof(MetadataType), fileProperties.type), fileProperties.fullName);
                        return itemsQuery.ToArray();
                    })
                    .ToArray();

            await Task.WhenAll(listMetadataTasks);

            var resultQuery =
                from listMetadataTask in listMetadataTasks
                from item in listMetadataTask.Result
                select item;

            return resultQuery.ToArray();
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

            var client = CreateClient();
            var response = await client.retrieveAsync(null, null, request);

            return response.result.id;
        }

        public async Task<RetrieveResult> GetRetrieveResultAsync(string operationId)
        {
            var client = CreateClient();
            var response = await client.checkRetrieveStatusAsync(null, null, operationId, includeZip: true);
            var result = response.result;

            if (result.status == ForceMetadata.RetrieveStatus.Failed)
                throw new RetrieveException(result.errorMessage, result.errorStatusCode.ToString());

            return new RetrieveResult(
                mMapper.Map<ForceMetadata.RetrieveStatus, RetrieveStatus>(result.status),
                result.zipFile);
        }

        public async Task<string> StartDeployAsync(byte[] zipFile)
        {
            var client = CreateClient();

            // TODO: Expose DeployOptions to API clients.
            var options = new DeployOptions() { checkOnly = false, rollbackOnError = true };
            var response = await client.deployAsync(null, null, null, zipFile, options);

            return response.result.id;
        }

        public async Task<DeployResult> GetDeployResultAsync(string operationId)
        {
            var client = CreateClient();
            var response = await client.checkDeployStatusAsync(null, null, operationId, includeDetails: true);
            var result = response.result;

            if (result.status == ForceMetadata.DeployStatus.Canceled)
                throw new DeployCanceledException(result.canceledByName);
            if (result.status == ForceMetadata.DeployStatus.Failed)
                throw new DeployException(result.errorMessage, result.errorStatusCode.ToString());

            return new DeployResult(
                mMapper.Map<ForceMetadata.DeployStatus, DeployStatus>(result.status),
                result.stateDetail,
                result.numberComponentsTotal,
                result.numberComponentsDeployed,
                result.numberComponentErrors,
                result.numberTestsTotal,
                result.numberTestsCompleted,
                result.numberTestErrors);
        }

        private MetadataPortTypeClient CreateClient()
        {
            var client = new MetadataPortTypeClient();

            var metadataEndpointUrl = String.Format(CultureInfo.InvariantCulture, Constants.MetadataEndpointUrlPattern, mInstanceUrl.ToString(), MetadataApiVersion);
            client.Endpoint.Address = new EndpointAddress(metadataEndpointUrl);
            client.Endpoint.SetSessionId(mSessionId);

            return client;
        }
    }
}
