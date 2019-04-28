using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using AutoMapper;
using IDeliverable.ForceClient.Core.OrgAccess;
using IDeliverable.ForceClient.Core.ServiceModel;
using IDeliverable.ForceClient.Metadata.Describe;
using IDeliverable.ForceClient.Metadata.ForceMetadata;
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
			mMapper = new MapperConfiguration(cfg => { }).CreateMapper();

			mCallPolicy = Policy.WrapAsync
			(
				 // Automatically refresh tokens if authorization fails.
				 Policy
					.Handle<FaultException>(ex => ex.Code.Name == "INVALID_SESSION_ID")
					.RetryAsync(
						retryCount: 1,
						onRetryAsync: async (outcome, retryNumber, context) =>
						{
							mLogger.LogInformation("Authorization with metadata API failed (API returned INVALID_SESSION_ID error); attempting to refresh access token.");
							_ = await mOrgAccessProvider.GetAccessTokenAsync(forceRefresh: true);
						}),

				 // Automatically retry 3 times on timeout.
				 Policy
					.Handle<TimeoutException>()
					.WaitAndRetryAsync(
						retryCount: 3,
						sleepDurationProvider: retryNumber =>
						{
							mLogger.LogWarning($"Timeout while calling metadata API; retrying #{retryNumber}.");
							return TimeSpan.FromSeconds(3);
						})
			);

			mClient.Endpoint.ConfigureOrgAccess(orgAccessProvider, mMetadataRules.MetadataApiName, mMetadataRules.MetadataApiVersion);
		}

		private readonly IOrgAccessProvider mOrgAccessProvider;
		private readonly MetadataRules mMetadataRules;
		private readonly ILogger mLogger;
		private readonly MetadataPortTypeClient mClient;
		private readonly IMapper mMapper;
		private readonly IAsyncPolicy mCallPolicy;

		public async Task<MetadataDescription> DescribeAsync()
		{
			try
			{
				var response = (await mCallPolicy.ExecuteAsync(() => mClient.describeMetadataAsync(null, null, mMetadataRules.MetadataApiVersion))).result;

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

			var folderQueries =
				types
					.Select(type => new ListMetadataQuery() { type = $"{type}{folderSuffix}" })
					.ToArray();

			try
			{
				var response = await mCallPolicy.ExecuteAsync(() => mClient.listMetadataAsync(null, null, folderQueries, mMetadataRules.MetadataApiVersion));

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

		public async Task<IEnumerable<MetadataItemInfo>> ListItemsAsync(IEnumerable<MetadataListQuery> queries, bool includePackages)
		{
			if (queries.Count() > mMetadataRules.MaxListMetadataQueriesPerRequest)
				throw new ArgumentOutOfRangeException(nameof(queries), $"The number of metadata queries ({queries.Count()}) is greater than the maximum number allowed per request ({mMetadataRules.MaxListMetadataQueriesPerRequest}).");

			var itemsQueries =
				queries
					.Select(query => new ListMetadataQuery() { type = query.Type.ToString(), folder = query.InFolder })
					.ToArray();

			try
			{
				var response = await mCallPolicy.ExecuteAsync(() => mClient.listMetadataAsync(null, null, itemsQueries, mMetadataRules.MetadataApiVersion));

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

		public async Task<string> StartRetrieveAsync(IEnumerable<MetadataRetrieveItemQuery> unpackedItemQueries, IEnumerable<string> packageNames)
		{
			if (unpackedItemQueries.Count() > mMetadataRules.MaxRetrieveMetadataItemsPerRequest)
				throw new ArgumentOutOfRangeException(nameof(unpackedItemQueries), $"The number of metadata items to retrieve ({unpackedItemQueries.Count()}) is greater than the maximum number allowed per request ({mMetadataRules.MaxRetrieveMetadataItemsPerRequest}).");

			var typeMembersQuery =
				from itemQuery in unpackedItemQueries
				group itemQuery.Name by itemQuery.Type into itemTypeGroup
				select new PackageTypeMembers()
				{
					name = itemTypeGroup.Key.ToString(),
					members = itemTypeGroup.ToArray()
				};

			var request = new RetrieveRequest()
			{
				unpackaged = new Package() { types = typeMembersQuery.ToArray() },
				packageNames = packageNames?.ToArray(),
				singlePackage = false
			};

			var response = await mCallPolicy.ExecuteAsync(() => mClient.retrieveAsync(null, null, request));

			return response.result.id;
		}

		public async Task<RetrieveResult> GetRetrieveResultAsync(string operationId)
		{
			var response = await mCallPolicy.ExecuteAsync(() => mClient.checkRetrieveStatusAsync(null, null, operationId, includeZip: true));
			var result = response.result;

			if (result.status == ForceMetadata.RetrieveStatus.Failed)
				throw new RetrieveException(result.errorMessage, result.errorStatusCode.ToString());

			return new RetrieveResult(
				mMapper.Map<ForceMetadata.RetrieveStatus, RetrieveStatus>(result.status),
				result.zipFile);
		}

		public async Task<string> StartDeployAsync(byte[] zipFile)
		{
			// TODO: Expose DeployOptions to API clients.
			var options = new DeployOptions() { checkOnly = false, rollbackOnError = true };
			var response = await mCallPolicy.ExecuteAsync(() => mClient.deployAsync(null, null, null, zipFile, options));

			return response.result.id;
		}

		public async Task<DeployResult> GetDeployResultAsync(string operationId)
		{
			var response = await mCallPolicy.ExecuteAsync(() => mClient.checkDeployStatusAsync(null, null, operationId, includeDetails: true));
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

		private async Task<DescribeValueTypeResult> DescribeTypeAsync(string type)
		{
			try
			{
				var fullType = "{http://soap.sforce.com/2006/04/metadata}" + type;
				var response = (await mCallPolicy.ExecuteAsync(() => mClient.describeValueTypeAsync(null, fullType))).result;

				return response;
			}
			catch (Exception ex)
			{
				mLogger.LogError(ex, $"Error while describing metadata type {type}.");
				throw;
			}
		}
	}
}
