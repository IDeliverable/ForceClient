using IDeliverable.ForceClient.Core;

namespace IDeliverable.ForceClient.Metadata.Processes.Retrieve
{
    public interface IRetrieveProcessFactory
    {
        IRetrieveProcess CreateRetrieveProcess(IOrgAccessProvider orgAccessProvider);
    }
}
