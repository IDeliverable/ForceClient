using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using IDeliverable.Utils.Core.CollectionExtensions;
using IDeliverable.Utils.Core.EventExtensions;

namespace IDeliverable.ForceClient.Metadata
{
    public class RetrieveWorker : INotifyPropertyChanged
    {
        public RetrieveWorker(MetadataGateway gateway)
        {
            mGateway = gateway;
        }

        private MetadataGateway mGateway;

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task<byte[]> RetrieveAllAsync(IEnumerable<MetadataType> types)
        {
            var retrieveItemReferences = await mGateway.ListItemsAsync(types);

			if (!retrieveItemReferences.Any())
				return new byte[] { };

			// This method support retrieving more metadata items than what the Metadata API
			// supports in one operation, by partitioning the list of items into chunks,
			// querying the API once per chunk, and then merging the resulting ZIP files into
			// one before returning.

			var retrieveTasks =
				retrieveItemReferences
					.Partition(MetadataGateway.MaxRetrieveMetadataItemsPerRequest)
					.Select(async (retrieveItemReferenceRange) =>
					{
						var operationId = await mGateway.StartRetrieveAsync(retrieveItemReferenceRange);
						RetrieveResult result = null;
						while (!(result = await mGateway.GetRetrieveResultAsync(operationId)).IsDone)
							await Task.Delay(TimeSpan.FromSeconds(3));
						return result;
					})
					.ToArray();

			await Task.WhenAll(retrieveTasks);

			if (retrieveTasks.Length == 1)
				return retrieveTasks.First().Result.ZipFile;

			using (var resultZipStream = new MemoryStream())
			{
				using (var resultZipArchive = new ZipArchive(resultZipStream, ZipArchiveMode.Create))
				{
					foreach (var retrieveTask in retrieveTasks)
					{
						using (var retrieveZipStream = new MemoryStream(retrieveTask.Result.ZipFile))
						{
							using (var retrieveZipArchive = new ZipArchive(retrieveZipStream, ZipArchiveMode.Read))
							{
								foreach (var retrieveZipEntry in retrieveZipArchive.Entries)
								{
									// A merged metadata ZIP file will not have any package manifests.
									if (retrieveZipEntry.Name == "package.xml")
										continue;

									var resultZipEntry = resultZipArchive.CreateEntry(retrieveZipEntry.FullName);

									using (Stream retrieveZipEntryStream = retrieveZipEntry.Open(), resultZipEntryStream = resultZipEntry.Open())
										await retrieveZipEntryStream.CopyToAsync(resultZipEntryStream);
								}
							}
						}
					}
				}

				return resultZipStream.ToArray();
			}
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.SafeRaise(ExceptionHandlingMode.Swallow, null, this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
