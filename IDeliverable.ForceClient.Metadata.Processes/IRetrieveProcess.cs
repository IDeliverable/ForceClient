using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Threading.Tasks;
using IDeliverable.ForceClient.Core.OrgAccess;

namespace IDeliverable.ForceClient.Metadata.Processes
{
	public interface IRetrieveProcess
	{
		Task<IEnumerable<MetadataItemInfo>> ListItemsOfTypesAsync(OrgType orgType, string username, IEnumerable<string> types, bool includePackages);
		Task<IReadOnlyDictionary<MetadataRetrieveItemQuery, bool>> RetrieveAsync(OrgType orgType, string username, IEnumerable<MetadataRetrieveItemQuery> itemReferences, Func<ZipArchiveEntry, Task> entryProcessorAsync);
	}
}
