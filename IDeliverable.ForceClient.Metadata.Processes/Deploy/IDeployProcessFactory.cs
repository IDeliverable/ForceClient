using IDeliverable.ForceClient.Core;

namespace IDeliverable.ForceClient.Metadata.Processes.Deploy
{
    public interface IDeployProcessFactory
    {
        IDeployProcess CreateDeployProcess(IOrgAccessProvider orgAccessProvider);
    }
}
