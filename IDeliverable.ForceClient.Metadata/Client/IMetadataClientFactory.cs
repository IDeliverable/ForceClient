using IDeliverable.ForceClient.Core.OrgAccess;

namespace IDeliverable.ForceClient.Metadata.Client
{
	public interface IMetadataClientFactory
	{
		IMetadataClient CreateClient(IOrgAccessProvider orgAccessProvider);
	}
}
