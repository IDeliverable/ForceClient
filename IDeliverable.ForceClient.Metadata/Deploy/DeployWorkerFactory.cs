using IDeliverable.ForceClient.Core;
using IDeliverable.ForceClient.Metadata.Client;

namespace IDeliverable.ForceClient.Metadata.Deploy
{
    public class DeployWorkerFactory : IDeployWorkerFactory
    {
        public DeployWorkerFactory(IMetadataClientFactory metadataClientFactory)
        {
            mMetadataClientFactory = metadataClientFactory;
        }

        private readonly IMetadataClientFactory mMetadataClientFactory;

        public IDeployWorker CreateDeployWorker(IOrgAccessProvider orgAccessProvider)
        {
            var client = mMetadataClientFactory.CreateClient(orgAccessProvider);
            var worker = new DeployWorker(client);
            return worker;
        }
    }
}
