using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Threading.Tasks;

namespace IDeliverable.ForceClient.Metadata.Processes.Retrieve
{
	public interface IRetrieveProcess
	{
		Task<IEnumerable<MetadataItemInfo>> ListItemsOfTypesAsync(IEnumerable<string> types, bool includePackages);
		Task<IReadOnlyDictionary<MetadataRetrieveItemQuery, bool>> RetrieveAsync(IEnumerable<MetadataRetrieveItemQuery> itemReferences, Func<ZipArchiveEntry, Task> entryProcessorAsync);
	}
}
