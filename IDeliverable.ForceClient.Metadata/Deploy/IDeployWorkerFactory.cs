using IDeliverable.ForceClient.Metadata.Client;

namespace IDeliverable.ForceClient.Metadata.Deploy
{
    public interface IDeployWorkerFactory
    {
        IDeployWorker CreateDeployWorker(IOrgAccessProvider orgAccessProvider);
    }
}
