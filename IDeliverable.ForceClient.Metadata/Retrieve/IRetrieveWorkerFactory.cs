using IDeliverable.ForceClient.Metadata.Client;

namespace IDeliverable.ForceClient.Metadata.Retrieve
{
    public interface IRetrieveWorkerFactory
    {
        IRetrieveWorker CreateRetrieveWorker(IOrgAccessProvider orgAccessProvider);
    }
}
