using System.Threading.Tasks;

namespace IDeliverable.ForceClient.Metadata.Deploy
{
	public interface IDeployWorker
	{
		Task<DeployResult> DeployAsync(byte[] zipFile);
	}
}
