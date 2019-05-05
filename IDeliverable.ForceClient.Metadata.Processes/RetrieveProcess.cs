using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using IDeliverable.ForceClient.Core.OrgAccess;
using IDeliverable.ForceClient.Metadata.Archives;
using IDeliverable.ForceClient.Metadata.Archives.Storage;
using IDeliverable.ForceClient.Metadata.Client;
using IDeliverable.Utils.Core.CollectionExtensions;
using Microsoft.Extensions.Logging;

namespace IDeliverable.ForceClient.Metadata.Processes
{
	public class RetrieveProcess : IRetrieveProcess
	{
		public RetrieveProcess(IMetadataClientFactory clientFactory, MetadataRules metadataRules, ILogger<RetrieveProcess> logger)
		{
			mClientFactory = clientFactory;
			mMetadataRules = metadataRules;
			mLogger = logger;
		}

		private readonly IMetadataClientFactory mClientFactory;
		private readonly MetadataRules mMetadataRules;
		private readonly ILogger mLogger;

		public async Task<IEnumerable<MetadataItemInfo>> ListItemsOfTypesAsync(OrgType orgType, string username, IEnumerable<string> types, bool includePackages)
		{
			var client = mClientFactory.CreateClient(orgType, username);
			var metadataDescription = await client.DescribeAsync();

			var result = new List<MetadataItemInfo>();

			var linkOptions = new DataflowLinkOptions() { PropagateCompletion = true };
			var parallelismOptions = new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = mMetadataRules.MaxConcurrentListMetadataRequests };

			var source = new BroadcastBlock<string>(type => type);
			var batchFolderTypes = new BatchBlock<string>(mMetadataRules.MaxListMetadataQueriesPerRequest);
			var listFolders = new TransformManyBlock<string[], MetadataFolderInfo>(typeList => client.ListFoldersAsync(typeList), parallelismOptions);
			var createFolderItemQueries = new TransformBlock<MetadataFolderInfo, MetadataListQuery>(folderInfo => new MetadataListQuery(folderInfo.ContainsType, folderInfo.Name));
			var createItemQueries = new TransformBlock<string, MetadataListQuery>(type => new MetadataListQuery(type));
			var batchItemQueries = new BatchBlock<MetadataListQuery>(mMetadataRules.MaxListMetadataQueriesPerRequest);
			var listItems = new TransformManyBlock<MetadataListQuery[], MetadataItemInfo>(queries => client.ListItemsAsync(queries, includePackages), parallelismOptions);
			var target = new ActionBlock<MetadataItemInfo>(itemInfo => result.Add(itemInfo));

			source.LinkTo(batchFolderTypes, linkOptions, type => metadataDescription.Types[type].IsFolderized);
			source.LinkTo(createItemQueries, linkOptions, type => !metadataDescription.Types[type].IsFolderized);
			batchFolderTypes.LinkTo(listFolders, linkOptions);
			listFolders.LinkTo(createFolderItemQueries, linkOptions);
			createFolderItemQueries.LinkTo(batchItemQueries); // These should not propagate completion
			createItemQueries.LinkTo(batchItemQueries); // These should not propagate completion
			batchItemQueries.LinkTo(listItems, linkOptions);
			listItems.LinkTo(target, linkOptions);

			// Whenever *both* item creation blocks are completed, only then can we signal
			// the completion of the target to which they are both linked. Perhaps there is
			// a more elegant way to specify in the linking that the target should complete
			// only when both sources are completed.
			_ = Task.WhenAll(createFolderItemQueries.Completion, createItemQueries.Completion).ContinueWith(_ => batchItemQueries.Complete());

			foreach (var type in types)
				source.Post(type);
			source.Complete();

			await target.Completion;

			return result;
		}

		public async Task<RetrieveResultInfo> RetrieveAsync(OrgType orgType, string username, IEnumerable<MetadataRetrieveItemQuery> itemQueries, Archive targetArchive, Func<IArchiveStorage> tempStorageFactory)
		{
			var result = new RetrieveResultInfo();

			if (!itemQueries.Any())
				return result;

			// This method support retrieving more metadata items than what the Metadata API
			// supports in one operation, by partitioning the list of items into chunks,
			// querying the API once per chunk, and then merging the resulting ZIP files into
			// one before returning.

			var itemQueryPartitions = itemQueries.Partition(mMetadataRules.MaxRetrieveMetadataItemsPerRequest);

			var numItemsTotal = itemQueries.Count();
			var numItemsRetrieved = 0;

			mLogger.LogInformation($"Retrieving {itemQueries.Count()} items in {itemQueryPartitions.Count()} batches...");

			var client = mClientFactory.CreateClient(orgType, username);
			var metadataDescription = await client.DescribeAsync();

			foreach (var itemQueryPartition in itemQueryPartitions)
			{
				RetrieveResult retrieveResult = null;

				try
				{
					var operationId = await client.StartRetrieveAsync(itemQueryPartition, packageNames: null, singlePackage: true);

					while (!(retrieveResult = await client.GetRetrieveResultAsync(operationId)).IsDone)
						await Task.Delay(TimeSpan.FromSeconds(3));
				}
				catch (Exception ex)
				{
					mLogger.LogError(ex, "Error during retrieve of a batch; output metadata will be incomplete.");
					numItemsRetrieved += itemQueryPartition.Count();
					//foreach (var itemReference in itemQueryPartition)
					//	result.Add(itemReference, false);
					continue;
				}

				var tempArchiveStorage = tempStorageFactory();
				await tempArchiveStorage.LoadFromZipAsync(retrieveResult.ZipFile);
				var tempArchive = await Archive.LoadAsync(tempArchiveStorage, metadataDescription);

				await targetArchive.MergeFromAsync(tempArchive);

				numItemsRetrieved += itemQueryPartition.Count();
				//foreach (var itemReference in itemQueryPartition)
				//	result.Add(itemReference, true);

				mLogger.LogInformation($"{numItemsRetrieved}/{numItemsTotal} items retrieved ({Decimal.Divide(numItemsRetrieved, numItemsTotal):P0})");
			}

			mLogger.LogInformation("All items successfully retrieved.");

			return result;
		}
	}
}
