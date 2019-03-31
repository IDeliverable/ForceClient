using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Threading.Tasks;
using IDeliverable.ForceClient.Metadata.Client;

namespace IDeliverable.ForceClient.Metadata.Retrieve
{
	public interface IRetrieveWorker
	{
		Task<IEnumerable<MetadataItemInfo>> ListItemsAsync(IEnumerable<MetadataType> types);
		Task<IEnumerable<MetadataItemInfo>> ListItemsAsync(IEnumerable<MetadataListQuery> queries);
		Task<IReadOnlyDictionary<MetadataItemInfo, bool>> RetrieveAsync(IEnumerable<MetadataItemInfo> itemReferences, Func<ZipArchiveEntry, Task> entryProcessorAsync);
		Task<IReadOnlyDictionary<MetadataItemInfo, bool>> RetrieveAsync(IEnumerable<MetadataItemInfo> itemReferences, string outputDirectoryPath);
	}
}
