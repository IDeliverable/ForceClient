using IDeliverable.ForceClient.Core.OrgAccess;
using IDeliverable.ForceClient.Metadata.Client;

namespace IDeliverable.ForceClient.Metadata.Processes.Deploy
{
	public class DeployProcessFactory : IDeployProcessFactory
	{
		public DeployProcessFactory(IMetadataClientFactory metadataClientFactory)
		{
			mMetadataClientFactory = metadataClientFactory;
		}

		private readonly IMetadataClientFactory mMetadataClientFactory;

		public IDeployProcess CreateDeployProcess(IOrgAccessProvider orgAccessProvider)
		{
			var client = mMetadataClientFactory.CreateClient(orgAccessProvider);
			var worker = new DeployProcess(client);
			return worker;
		}
	}
}
