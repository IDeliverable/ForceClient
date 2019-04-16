using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Threading.Tasks;

namespace IDeliverable.ForceClient.Metadata.Processes.Retrieve
{
	public interface IRetrieveProcess
	{
		Task<IEnumerable<MetadataItemInfo>> ListItemsAsync(IEnumerable<string> types);
		Task<IEnumerable<MetadataItemInfo>> ListItemsAsync(IEnumerable<MetadataListQuery> queries);
		Task<IReadOnlyDictionary<MetadataRetrieveQuery, bool>> RetrieveAsync(IEnumerable<MetadataRetrieveQuery> itemReferences, Func<ZipArchiveEntry, Task> entryProcessorAsync);
		Task<IReadOnlyDictionary<MetadataRetrieveQuery, bool>> RetrieveAsync(IEnumerable<MetadataRetrieveQuery> itemReferences, string outputDirectoryPath);
	}
}
