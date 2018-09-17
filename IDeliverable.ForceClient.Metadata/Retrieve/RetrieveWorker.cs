using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using IDeliverable.Utils.Core.CollectionExtensions;
using Microsoft.Extensions.Logging;

namespace IDeliverable.ForceClient.Metadata.Retrieve
{
    public class RetrieveWorker
    {
        public RetrieveWorker(MetadataGateway gateway, ILogger<RetrieveWorker> logger)
        {
            mGateway = gateway;
            mLogger = logger;
        }

        private readonly MetadataGateway mGateway;
        private readonly ILogger mLogger;

        public async Task<IReadOnlyDictionary<MetadataItemReference, bool>> RetrieveAsync(IEnumerable<MetadataItemReference> itemReferences, string outputDirectoryPath)
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

        public async Task<IReadOnlyDictionary<MetadataItemReference, bool>> RetrieveAsync(IEnumerable<MetadataItemReference> itemReferences, Func<ZipArchiveEntry, Task> entryProcessorAsync)
        {
            var result = new Dictionary<MetadataItemReference, bool>();

            if (!itemReferences.Any())
                return result;

            // This method support retrieving more metadata items than what the Metadata API
            // supports in one operation, by partitioning the list of items into chunks,
            // querying the API once per chunk, and then merging the resulting ZIP files into
            // one before returning.

            var itemReferencePartitions = itemReferences.Partition(MetadataGateway.MaxRetrieveMetadataItemsPerRequest);

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
                    var operationId = await mGateway.StartRetrieveAsync(itemReferencePartition);

                    while (!(retrieveResult = await mGateway.GetRetrieveResultAsync(operationId)).IsDone)
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
