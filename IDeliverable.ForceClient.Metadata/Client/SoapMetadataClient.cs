using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using AutoMapper;
using IDeliverable.ForceClient.Core;
using IDeliverable.ForceClient.Metadata.Deploy;
using IDeliverable.ForceClient.Metadata.Describe;
using IDeliverable.ForceClient.Metadata.ForceMetadata;
using IDeliverable.ForceClient.Metadata.Retrieve;
using Microsoft.Extensions.Logging;
using Polly;

namespace IDeliverable.ForceClient.Metadata.Client
{
    class SoapMetadataClient : IMetadataClient
    {
        public SoapMetadataClient(IOrgAccessProvider orgAccessProvider, MetadataRules metadataRules, ILogger<SoapMetadataClient> logger)
        {
            mOrgAccessProvider = orgAccessProvider;
            mMetadataRules = metadataRules;
            mLogger = logger;

            mClient = new MetadataPortTypeClient();
            mClient.Endpoint.Address = null;
            mMapper = new MapperConfiguration(cfg => { }).CreateMapper();
            mRetryPolicy = Policy.Handle<TimeoutException>().WaitAndRetryAsync(3, x => TimeSpan.FromSeconds(3));
        }

        private readonly IOrgAccessProvider mOrgAccessProvider;
        private readonly MetadataRules mMetadataRules;
        private readonly ILogger mLogger;
        private readonly MetadataPortTypeClient mClient;
        private readonly IMapper mMapper;
        private readonly IAsyncPolicy mRetryPolicy;

        public async Task<MetadataDescription> DescribeAsync()
        {
            try
            {
                await EnsureClientHasEndpointAddressAsync();
                var header = await GetAuthenticationHeaderAsync();
                var response = (await mRetryPolicy.ExecuteAsync(() => mClient.describeMetadataAsync(header, null, mMetadataRules.MetadataApiVersion))).result;

                var types = new Dictionary<string, MetadataTypeDescription>();

                foreach (var metadataObject in response.metadataObjects)
                {
                    IEnumerable<NestedMetadataTypeDescription> nestedTypesQuery = null;
                    if (metadataObject.childXmlNames != null && metadataObject.childXmlNames.Any())
                    {
                        var metadataObjectDetails = await DescribeTypeAsync(metadataObject.xmlName);
                        nestedTypesQuery =
                            from nestedTypeName in metadataObject.childXmlNames
                            let element =
                                metadataObjectDetails.valueTypeFields
                                    .Where(x => !(x.soapType == "CustomField" && x.name == "nameField")) // A known case where we can't reliably determine name to XML element name mapping.
                                    .SingleOrDefault(x => x.soapType == nestedTypeName)
                            where element != null
                            let elementName = element.name
                            let keyChildElementName = element.fields.Single(x => x.isNameField).name
                            select new NestedMetadataTypeDescription(nestedTypeName, elementName, keyChildElementName);
                    }

                    types.Add(metadataObject.xmlName, new MetadataTypeDescription(metadataObject.xmlName, metadataObject.directoryName, metadataObject.suffix, metadataObject.inFolder, metadataObject.metaFile, nestedTypesQuery?.ToArray()));
                }

                var result = new MetadataDescription(response.organizationNamespace, response.partialSaveAllowed, response.testRequired, types);

                return result;
            }
            catch (Exception ex)
            {
                mLogger.LogError(ex, "Error while describing org metadata.");
                if (ex is FaultException fex)
                    throw new MetadataException(fex.Reason.ToString(), fex.Code.Name, fex);
                throw;
            }
        }

        public async Task<IEnumerable<MetadataFolderInfo>> ListFoldersAsync(IEnumerable<string> types)
        {
            var folderSuffix = "Folder";

            if (types.Count() > mMetadataRules.MaxListMetadataQueriesPerRequest)
                throw new ArgumentOutOfRangeException(nameof(types), $"The number of metadata types ({types.Count()}) is greater than the maximum number allowed per request ({mMetadataRules.MaxListMetadataQueriesPerRequest}).");

            //if (types.Any(type => !mMetadataRules.GetIsFolderized(type)))
            //    throw new ArgumentException("One or more of the specified metadata types is not a folderized type.", nameof(types));

            var folderQueries =
                types
                    .Select(type => new ListMetadataQuery() { type = $"{type}{folderSuffix}" })
                    .ToArray();

            try
            {
                await EnsureClientHasEndpointAddressAsync();
                var header = await GetAuthenticationHeaderAsync();
                var response = await mRetryPolicy.ExecuteAsync(() => mClient.listMetadataAsync(header, null, folderQueries, mMetadataRules.MetadataApiVersion));

                var folderInfoQuery =
                    from fileProperties in response.result
                    let type = fileProperties.type.EndsWith(folderSuffix) ? fileProperties.type.Substring(0, fileProperties.type.Length - folderSuffix.Length) : fileProperties.type
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

        public async Task<IEnumerable<MetadataItemInfo>> ListItemsAsync(IEnumerable<MetadataListQuery> queries, bool includePackages = false)
        {
            if (queries.Count() > mMetadataRules.MaxListMetadataQueriesPerRequest)
                throw new ArgumentOutOfRangeException(nameof(queries), $"The number of metadata queries ({queries.Count()}) is greater than the maximum number allowed per request ({mMetadataRules.MaxListMetadataQueriesPerRequest}).");

            //if (queries.Any(query => mMetadataRules.GetIsFolderized(query.Type) && String.IsNullOrEmpty(query.InFolder)))
            //    throw new ArgumentException($"One or more of the specified metadata types is a folderized type but no folder is specified; use {nameof(ListFoldersAsync)} first to obtain the available folders.", nameof(queries));

            var itemsQueries =
                queries
                    .Select(query => new ListMetadataQuery() { type = query.Type.ToString(), folder = query.InFolder })
                    .ToArray();

            try
            {
                await EnsureClientHasEndpointAddressAsync();
                var header = await GetAuthenticationHeaderAsync();
                var response = await mRetryPolicy.ExecuteAsync(() => mClient.listMetadataAsync(header, null, itemsQueries, mMetadataRules.MetadataApiVersion));

                // Result can sometimes be null for empty folders.
                if (response?.result == null)
                    return Enumerable.Empty<MetadataItemInfo>();

                var itemInfoQuery =
                    from fileProperties in response.result
                    where !String.IsNullOrEmpty(fileProperties.type) // Why are we filtering on this?
                    where includePackages || !fileProperties.IsInPackage()
                    select fileProperties.AsMetadataItemInfo();
                var itemInfoList = itemInfoQuery.ToArray();

                return itemInfoList;
            }
            catch (FaultException ex) when (ex.Code.Name == "INVALID_TYPE")
            {
                mLogger.LogWarning(ex.Message);
                return Enumerable.Empty<MetadataItemInfo>();
            }
            catch (Exception ex)
            {
                mLogger.LogError(ex, "Error while listing metadata items.");
                if (ex is FaultException fex)
                    throw new MetadataException(fex.Reason.ToString(), fex.Code.Name, fex);
                throw;
            }
        }

        public async Task<string> StartRetrieveAsync(IEnumerable<MetadataRetrieveQuery> items)
        {
            if (items.Count() > mMetadataRules.MaxRetrieveMetadataItemsPerRequest)
                throw new ArgumentOutOfRangeException(nameof(items), $"The number of metadata items to retrieve ({items.Count()}) is greater than the maximum number allowed per request ({mMetadataRules.MaxRetrieveMetadataItemsPerRequest}).");

            var typeMembersQuery =
                from itemReference in items
                group itemReference.Name by itemReference.Type into itemTypeGroup
                select new PackageTypeMembers() { name = itemTypeGroup.Key.ToString(), members = itemTypeGroup.ToArray() };

            var package = new Package() { types = typeMembersQuery.ToArray() };
            var request = new RetrieveRequest() { unpackaged = package, singlePackage = false };

            await EnsureClientHasEndpointAddressAsync();
            var header = await GetAuthenticationHeaderAsync();
            var response = await mRetryPolicy.ExecuteAsync(() => mClient.retrieveAsync(header, null, request));

            return response.result.id;
        }

        public async Task<Retrieve.RetrieveResult> GetRetrieveResultAsync(string operationId)
        {
            await EnsureClientHasEndpointAddressAsync();
            var header = await GetAuthenticationHeaderAsync();
            var response = await mRetryPolicy.ExecuteAsync(() => mClient.checkRetrieveStatusAsync(header, null, operationId, includeZip: true));
            var result = response.result;

            if (result.status == ForceMetadata.RetrieveStatus.Failed)
                throw new RetrieveException(result.errorMessage, result.errorStatusCode.ToString());

            return new Retrieve.RetrieveResult(
                mMapper.Map<ForceMetadata.RetrieveStatus, Retrieve.RetrieveStatus>(result.status),
                result.zipFile);
        }

        public async Task<string> StartDeployAsync(byte[] zipFile)
        {
            await EnsureClientHasEndpointAddressAsync();
            var header = await GetAuthenticationHeaderAsync();
            // TODO: Expose DeployOptions to API clients.
            var options = new DeployOptions() { checkOnly = false, rollbackOnError = true };
            var response = await mRetryPolicy.ExecuteAsync(() => mClient.deployAsync(header, null, null, zipFile, options));

            return response.result.id;
        }

        public async Task<Deploy.DeployResult> GetDeployResultAsync(string operationId)
        {
            await EnsureClientHasEndpointAddressAsync();
            var header = await GetAuthenticationHeaderAsync();
            var response = await mRetryPolicy.ExecuteAsync(() => mClient.checkDeployStatusAsync(header, null, operationId, includeDetails: true));
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

        private async Task<DescribeValueTypeResult> DescribeTypeAsync(string type)
        {
            try
            {
                var header = await GetAuthenticationHeaderAsync();
                var fullType = "{http://soap.sforce.com/2006/04/metadata}" + type;
                var response = (await mRetryPolicy.ExecuteAsync(() => mClient.describeValueTypeAsync(header, fullType))).result;

                return response;
            }
            catch (Exception ex)
            {
                mLogger.LogError(ex, $"Error while describing metadata type {type}.");
                throw;
            }
        }

        private async Task EnsureClientHasEndpointAddressAsync()
        {
            if (mClient.Endpoint.Address != null)
                return;
            var soapUrl = await mOrgAccessProvider.GetSoapUrlAsync();
            var endpointAddress = new EndpointAddress(new Uri(soapUrl.Replace("{version}", mMetadataRules.MetadataApiVersion.ToString(CultureInfo.InvariantCulture))));
            mClient.Endpoint.Address = endpointAddress;
        }

        private async Task<SessionHeader> GetAuthenticationHeaderAsync()
        {
            var accessToken = await mOrgAccessProvider.GetAccessTokenAsync();
            var header = new SessionHeader() { sessionId = accessToken };
            return header;
        }
    }
}
