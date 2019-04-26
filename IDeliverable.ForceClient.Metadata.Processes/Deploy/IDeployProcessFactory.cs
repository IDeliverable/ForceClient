using IDeliverable.ForceClient.Core.OrgAccess;

namespace IDeliverable.ForceClient.Metadata.Processes.Deploy
{
    public interface IDeployProcessFactory
    {
        IDeployProcess CreateDeployProcess(IOrgAccessProvider orgAccessProvider);
    }
}
