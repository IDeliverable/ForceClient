using System.Threading.Tasks;
using IDeliverable.ForceClient.Metadata.Client;

namespace IDeliverable.ForceClient.Metadata.Processes.Deploy
{
	public interface IDeployProcess
	{
		Task<DeployResult> DeployAsync(byte[] zipFile);
	}
}
