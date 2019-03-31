using System.Threading.Tasks;

namespace IDeliverable.ForceClient.Metadata.Client
{
    public interface IOrgAccessProvider
    {
        Task<string> GetSoapUrlAsync();
        Task<string> GetAccessTokenAsync();
    }
}
