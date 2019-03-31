using System.Threading.Tasks;

namespace IDeliverable.ForceClient.Core
{
    public interface IOrgAccessProvider
    {
        Task<string> GetSoapUrlAsync();
        Task<string> GetAccessTokenAsync();
    }
}
