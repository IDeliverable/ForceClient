using IDeliverable.ForceClient.Core;

namespace IDeliverable.ForceClient.Metadata.Retrieve
{
    public interface IRetrieveWorkerFactory
    {
        IRetrieveWorker CreateRetrieveWorker(IOrgAccessProvider orgAccessProvider);
    }
}
