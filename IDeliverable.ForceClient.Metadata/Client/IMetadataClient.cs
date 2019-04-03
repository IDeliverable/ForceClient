using System.Collections.Generic;
using System.Threading.Tasks;
using IDeliverable.ForceClient.Metadata.Deploy;
using IDeliverable.ForceClient.Metadata.Retrieve;

namespace IDeliverable.ForceClient.Metadata.Client
{
    public interface IMetadataClient
    {
        Task<IEnumerable<MetadataFolderInfo>> ListFoldersAsync(IEnumerable<MetadataType> types);
        Task<IEnumerable<MetadataItemInfo>> ListItemsAsync(IEnumerable<MetadataListQuery> queries, bool includePackages = false);
        Task<string> StartRetrieveAsync(IEnumerable<MetadataRetrieveSpec> items);
        Task<RetrieveResult> GetRetrieveResultAsync(string operationId);
        Task<string> StartDeployAsync(byte[] zipFile);
        Task<DeployResult> GetDeployResultAsync(string operationId);
    }
}
