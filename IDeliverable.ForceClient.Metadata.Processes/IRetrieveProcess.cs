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
		Task<RetrieveResultInfo> RetrieveAsync(OrgType orgType, string username, IEnumerable<MetadataRetrieveItemQuery> itemQueries, Archive targetArchive, Func<IArchiveStorage> tempStorageFactory);
	}
}
