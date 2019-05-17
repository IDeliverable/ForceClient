using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IDeliverable.ForceClient.Core.OrgAccess;
using IDeliverable.ForceClient.Metadata.Archives;
using IDeliverable.ForceClient.Metadata.Archives.Storage;

namespace IDeliverable.ForceClient.Metadata.Processes
{
	public interface IRetrieveProcess
	{
		Task<IEnumerable<MetadataItemInfo>> ListItemsOfTypesAsync(OrgType orgType, string username, IEnumerable<string> types, bool includePackages);
		Task RetrieveAsync(OrgType orgType, string username, IEnumerable<MetadataRetrieveItemQuery> unpackagedItemQueries, IEnumerable<string> packageNames, Archive targetArchive, Func<IArchiveStorage> tempStorageFactory, int batchSize);
	}
}
