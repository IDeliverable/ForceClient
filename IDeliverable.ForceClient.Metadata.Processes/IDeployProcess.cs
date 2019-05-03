using System.Threading.Tasks;
using IDeliverable.ForceClient.Core.OrgAccess;
using IDeliverable.ForceClient.Metadata.Client;

namespace IDeliverable.ForceClient.Metadata.Processes
{
	public interface IDeployProcess
	{
		Task<DeployResult> DeployAsync(OrgType orgType, string username, byte[] zipFile);
	}
}
