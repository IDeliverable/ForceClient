using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using IDeliverable.ForceClient.Core.OrgAccess;
using IDeliverable.ForceClient.Metadata.Archives;
using IDeliverable.ForceClient.Metadata.Archives.Storage;
using IDeliverable.ForceClient.Metadata.Client;
using IDeliverable.ForceClient.Metadata.Describe;
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

			return
				result
					.Distinct()
					.OrderBy(x => x.Type)
					.ThenBy(x => x.Name)
					.ThenBy(x => x.Id)
					.ToArray();
		}

		public async Task RetrieveAsync(OrgType orgType, string username, IEnumerable<MetadataRetrieveItemQuery> unpackagedItemQueries, IEnumerable<string> packageNames, Archive targetArchive, Func<IArchiveStorage> tempStorageFactory, int batchSize)
		{
			if (batchSize < 1 || batchSize > mMetadataRules.MaxRetrieveMetadataItemsPerRequest)
				throw new ArgumentOutOfRangeException(nameof(batchSize), $"Argument '{nameof(batchSize)}' must be between 1 and {mMetadataRules.MaxRetrieveMetadataItemsPerRequest}.");

			unpackagedItemQueries = unpackagedItemQueries ?? new MetadataRetrieveItemQuery[] { };
			packageNames = packageNames ?? new string[] { };

			if (!unpackagedItemQueries.Any() && !packageNames.Any())
				return;

			var retrieveBatch = new TransformBlock<BatchInfo, BatchInfo>(RetrieveBatchAsync, Parallelism(mMetadataRules.MaxConcurrentRetrieveMetadataRequests));
			var createTempArchive = new TransformBlock<BatchInfo, BatchInfo>(CreateTempArchiveAsync, Parallelism(Environment.ProcessorCount));
			var mergeTempToTargetArchive = new ActionBlock<BatchInfo>(MergeTempToTargetArchiveAsync);

			var linkOptions = new DataflowLinkOptions() { PropagateCompletion = true };
			retrieveBatch.LinkTo(createTempArchive, linkOptions);
			createTempArchive.LinkTo(mergeTempToTargetArchive, linkOptions);
			
			var client = mClientFactory.CreateClient(orgType, username);
			var metadataDescription = await client.DescribeAsync();

			var batchNumber = 1;

			if (unpackagedItemQueries.Any())
			{
				var itemQueryPartitions = unpackagedItemQueries.Partition(batchSize);
				mLogger.LogInformation($"Retrieving {unpackagedItemQueries.Count()} unpackaged items in {itemQueryPartitions.Count()} batches...");
				foreach (var itemQueryPartition in itemQueryPartitions)
					retrieveBatch.Post(new BatchInfo(batchNumber++, client, metadataDescription, targetArchive, tempStorageFactory, itemQueryPartition));
			}

			if (packageNames.Any())
			{
				mLogger.LogInformation($"Retrieving {packageNames.Count()} packages...");
				foreach (var packageName in packageNames)
					retrieveBatch.Post(new BatchInfo(batchNumber++, client, metadataDescription, targetArchive, tempStorageFactory, packageName));
			}

			retrieveBatch.Complete();

			await mergeTempToTargetArchive.Completion;
		}

		private async Task<BatchInfo> RetrieveBatchAsync(BatchInfo batchInfo)
		{
			mLogger.LogInformation($"Retrieving batch #{batchInfo.BatchNumber}...");

			var sw = new Stopwatch();
			sw.Start();

			var packageNames = batchInfo.PackageName != null ? new string[] { batchInfo.PackageName } : null;
			var operationId = await batchInfo.MetadataClient.StartRetrieveAsync(batchInfo.UnpackagedItemQueries, packageNames, singlePackage: true);

			RetrieveResult retrieveResult;
			while (!(retrieveResult = await batchInfo.MetadataClient.GetRetrieveResultAsync(operationId)).IsDone)
				await Task.Delay(TimeSpan.FromSeconds(3));

			batchInfo.ZipFile = retrieveResult.ZipFile;

			sw.Stop();

			mLogger.LogInformation($"Batch #{batchInfo.BatchNumber} successfully retrieved in {sw.Elapsed.TotalSeconds:F3} seconds.");

			return batchInfo;
		}

		private async Task<BatchInfo> CreateTempArchiveAsync(BatchInfo batchInfo)
		{
			mLogger.LogInformation($"Extracting batch #{batchInfo.BatchNumber}...");

			var tempArchiveStorage = batchInfo.CreateTempStorage();
			await tempArchiveStorage.LoadFromZipAsync(batchInfo.ZipFile);
			batchInfo.TempArchive = await Archive.LoadAsync(tempArchiveStorage, batchInfo.MetadataDescription);

			mLogger.LogInformation($"Batch #{batchInfo.BatchNumber} successfully extracted.");

			return batchInfo;
		}

		private async Task MergeTempToTargetArchiveAsync(BatchInfo batchInfo)
		{
			mLogger.LogInformation($"Merging batch #{batchInfo.BatchNumber} into target archive...");

			await batchInfo.TargetArchive.MergeFromAsync(batchInfo.TempArchive);

			mLogger.LogInformation($"Batch #{batchInfo.BatchNumber} successfully merged into target archive.");
		}

		private ExecutionDataflowBlockOptions Parallelism(int maxDegreeOfParallelism)
		{
			return new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = maxDegreeOfParallelism, EnsureOrdered = false };
		}

		private struct BatchInfo
		{
			public BatchInfo(int batchNumber, IMetadataClient metadataClient, MetadataDescription metadataDescription, Archive targetArchive, Func<IArchiveStorage> tempStorageFactory, IEnumerable<MetadataRetrieveItemQuery> unpackagedItemQueries)
			{
				if (unpackagedItemQueries == null || !unpackagedItemQueries.Any())
					throw new ArgumentException("No unpackaged item queries specified.");

				BatchNumber = batchNumber;
				MetadataClient = metadataClient;
				MetadataDescription = metadataDescription;
				TargetArchive = targetArchive;
				CreateTempStorage = tempStorageFactory;
				UnpackagedItemQueries = unpackagedItemQueries;
				PackageName = null;

				ZipFile = null;
				TempArchive = null;
			}

			public BatchInfo(int batchNumber, IMetadataClient metadataClient, MetadataDescription metadataDescription, Archive targetArchive, Func<IArchiveStorage> tempStorageFactory, string packageName)
			{
				if (String.IsNullOrEmpty(packageName))
					throw new ArgumentException("No package name specified.");

				BatchNumber = batchNumber;
				MetadataClient = metadataClient;
				MetadataDescription = metadataDescription;
				TargetArchive = targetArchive;
				CreateTempStorage = tempStorageFactory;
				UnpackagedItemQueries = null;
				PackageName = packageName;

				ZipFile = null;
				TempArchive = null;
			}

			public readonly int BatchNumber;
			public readonly IMetadataClient MetadataClient;
			public readonly MetadataDescription MetadataDescription;
			public readonly Archive TargetArchive;
			public readonly Func<IArchiveStorage> CreateTempStorage;
			public readonly IEnumerable<MetadataRetrieveItemQuery> UnpackagedItemQueries;
			public readonly string PackageName;

			public byte[] ZipFile;
			public Archive TempArchive;
		}
	}
}
