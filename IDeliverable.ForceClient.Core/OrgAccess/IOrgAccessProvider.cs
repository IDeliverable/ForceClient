using System.Threading.Tasks;

namespace IDeliverable.ForceClient.Core.OrgAccess
{
	public interface IOrgAccessProvider
	{
		Task<string> GetSoapApiUrlAsync(OrgType orgType, string username, string apiName);
		Task<string> GetAccessTokenAsync(OrgType orgType, string username, bool forceRefresh);
	}
}
