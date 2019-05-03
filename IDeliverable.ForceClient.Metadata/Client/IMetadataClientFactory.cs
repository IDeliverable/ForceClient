using IDeliverable.ForceClient.Core.OrgAccess;

namespace IDeliverable.ForceClient.Metadata.Client
{
	public interface IMetadataClientFactory
	{
		IMetadataClient CreateClient(OrgType orgType, string username);
	}
}
