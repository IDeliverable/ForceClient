using System.Threading.Tasks;

namespace IDeliverable.ForceClient.Core.OrgAccess
{
    public interface IOrgAccessProvider
    {
        Task<string> GetSoapApiUrlAsync(string apiName);
        Task<string> GetAccessTokenAsync();
    }
}
