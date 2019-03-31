using IDeliverable.ForceClient.Core;

namespace IDeliverable.ForceClient.Metadata.Client
{
    public interface IMetadataClientFactory
    {
        IMetadataClient CreateClient(IOrgAccessProvider orgAccessProvider);
    }
}
