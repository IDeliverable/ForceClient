using System.Collections.Generic;
using System.Threading.Tasks;
using IDeliverable.ForceClient.Metadata.Describe;

namespace IDeliverable.ForceClient.Metadata.Client
{
    public interface IMetadataClient
    {
        Task<MetadataDescription> DescribeAsync();
        Task<IEnumerable<MetadataFolderInfo>> ListFoldersAsync(IEnumerable<string> types);
        Task<IEnumerable<MetadataItemInfo>> ListItemsAsync(IEnumerable<MetadataListQuery> queries, bool includePackages = false);
        Task<string> StartRetrieveAsync(IEnumerable<MetadataRetrieveQuery> items);
        Task<RetrieveResult> GetRetrieveResultAsync(string operationId);
        Task<string> StartDeployAsync(byte[] zipFile);
        Task<DeployResult> GetDeployResultAsync(string operationId);
    }
}
