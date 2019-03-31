using IDeliverable.ForceClient.Core;

namespace IDeliverable.ForceClient.Metadata.Deploy
{
    public interface IDeployWorkerFactory
    {
        IDeployWorker CreateDeployWorker(IOrgAccessProvider orgAccessProvider);
    }
}
