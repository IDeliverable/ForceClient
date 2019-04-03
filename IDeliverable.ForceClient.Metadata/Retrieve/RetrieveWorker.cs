using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using IDeliverable.ForceClient.Metadata.Client;
using IDeliverable.Utils.Core.CollectionExtensions;
using Microsoft.Extensions.Logging;

namespace IDeliverable.ForceClient.Metadata.Retrieve
{
    public class RetrieveWorker : IRetrieveWorker
    {
        public RetrieveWorker(IMetadataClient client, MetadataRules metadataRules, ILogger<RetrieveWorker> logger)
        {
            mClient = client;
            mMetadataRules = metadataRules;
            mLogger = logger;
        }

        private readonly IMetadataClient mClient;
        private readonly MetadataRules mMetadataRules;
        private readonly ILogger mLogger;

        public async Task<IEnumerable<MetadataItemInfo>> ListItemsAsync(IEnumerable<MetadataType> types)
        {
            var result = new List<MetadataItemInfo>();

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            var parallelismOptions = new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = mMetadataRules.MaxConcurrentListMetadataRequests };

            var source = new BroadcastBlock<MetadataType>(type => type);
            var batchFolderTypes = new BatchBlock<MetadataType>(mMetadataRules.MaxListMetadataQueriesPerRequest);
            var listFolders = new TransformManyBlock<MetadataType[], MetadataFolderInfo>(typeList => mClient.ListFoldersAsync(typeList), parallelismOptions);
            var createFolderItemQueries = new TransformBlock<MetadataFolderInfo, MetadataListQuery>(folderInfo => new MetadataListQuery(folderInfo.ContainsType, folderInfo.Name));
            var createItemQueries = new TransformBlock<MetadataType, MetadataListQuery>(type => new MetadataListQuery(type));
            var batchItemQueries = new BatchBlock<MetadataListQuery>(mMetadataRules.MaxListMetadataQueriesPerRequest);
            var listItems = new TransformManyBlock<MetadataListQuery[], MetadataItemInfo>(queries => mClient.ListItemsAsync(queries), parallelismOptions);
            var target = new ActionBlock<MetadataItemInfo>(itemInfo => result.Add(itemInfo));

            source.LinkTo(batchFolderTypes, linkOptions, type => mMetadataRules.GetIsFolderized(type));
            source.LinkTo(createItemQueries, linkOptions, type => !mMetadataRules.GetIsFolderized(type));
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

        public async Task<IEnumerable<MetadataItemInfo>> ListItemsAsync(IEnumerable<MetadataListQuery> queries)
        {
            return await mClient.ListItemsAsync(queries);
        }

        public async Task<IReadOnlyDictionary<MetadataRetrieveSpec, bool>> RetrieveAsync(IEnumerable<MetadataRetrieveSpec> itemReferences, string outputDirectoryPath)
        {
            Directory.CreateDirectory(outputDirectoryPath);

            async Task entryProcessorAsync(ZipArchiveEntry entry)
            {
                var fullPath = Path.Combine(outputDirectoryPath, entry.FullName);

                var directoryPath = Path.GetDirectoryName(fullPath);
                Directory.CreateDirectory(directoryPath);

                using (Stream fileStream = File.Create(fullPath), entryStream = entry.Open())
                    await entryStream.CopyToAsync(fileStream);
            }

            return await RetrieveAsync(itemReferences, entryProcessorAsync);
        }

        public async Task<IReadOnlyDictionary<MetadataRetrieveSpec, bool>> RetrieveAsync(IEnumerable<MetadataRetrieveSpec> itemReferences, Func<ZipArchiveEntry, Task> entryProcessorAsync)
        {
            var result = new Dictionary<MetadataRetrieveSpec, bool>();

            if (!itemReferences.Any())
                return result;

            // This method support retrieving more metadata items than what the Metadata API
            // supports in one operation, by partitioning the list of items into chunks,
            // querying the API once per chunk, and then merging the resulting ZIP files into
            // one before returning.

            var itemReferencePartitions = itemReferences.Partition(mMetadataRules.MaxRetrieveMetadataItemsPerRequest);

            var numItemsTotal = itemReferences.Count();
            var numItemsRetrieved = 0;

            mLogger.LogInformation($"Retrieving {itemReferences.Count()} items in {itemReferencePartitions.Count()} batches...");

            //using (var resultZipArchive = new ZipArchive(outputStream, ZipArchiveMode.Create))
            //{
            foreach (var itemReferencePartition in itemReferencePartitions)
            {
                RetrieveResult retrieveResult = null;

                try
                {
                    var operationId = await mClient.StartRetrieveAsync(itemReferencePartition);

                    while (!(retrieveResult = await mClient.GetRetrieveResultAsync(operationId)).IsDone)
                        await Task.Delay(TimeSpan.FromSeconds(3));
                }
                catch (Exception ex)
                {
                    mLogger.LogError(ex, "Error during retrieve of a batch; output metadata will be incomplete.");
                    numItemsRetrieved += itemReferencePartition.Count();
                    foreach (var itemReference in itemReferencePartition)
                        result.Add(itemReference, false);
                    continue;
                }

                using (var retrieveZipStream = new MemoryStream(retrieveResult.ZipFile))
                {
                    using (var retrieveZipArchive = new ZipArchive(retrieveZipStream, ZipArchiveMode.Read))
                    {
                        foreach (var retrieveZipEntry in retrieveZipArchive.Entries)
                        {
                            // A merged metadata ZIP file will not have any package manifests.
                            if (retrieveZipEntry.Name == "package.xml")
                                continue;

                            await entryProcessorAsync(retrieveZipEntry);

                            //var resultZipEntry = resultZipArchive.CreateEntry(retrieveZipEntry.FullName);

                            //using (Stream retrieveZipEntryStream = retrieveZipEntry.Open(), resultZipEntryStream = resultZipEntry.Open())
                            //    await retrieveZipEntryStream.CopyToAsync(resultZipEntryStream);
                        }
                    }
                }

                numItemsRetrieved += itemReferencePartition.Count();
                foreach (var itemReference in itemReferencePartition)
                    result.Add(itemReference, true);

                mLogger.LogInformation($"{numItemsRetrieved}/{numItemsTotal} items retrieved ({Decimal.Divide(numItemsRetrieved, numItemsTotal):P0})");
            }
            //}

            mLogger.LogInformation("All items successfully retrieved.");

            return result;
        }
    }
}
