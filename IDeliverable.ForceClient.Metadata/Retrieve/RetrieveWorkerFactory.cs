using System;
using IDeliverable.ForceClient.Metadata.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IDeliverable.ForceClient.Metadata.Retrieve
{
    public class RetrieveWorkerFactory : IRetrieveWorkerFactory
    {
        public RetrieveWorkerFactory(IMetadataClientFactory metadataClientFactory, MetadataRules metadataRules, IServiceProvider serviceProvider)
        {
            mMetadataClientFactory = metadataClientFactory;
            mMetadataRules = metadataRules;
            mService = serviceProvider;
        }

        private readonly IMetadataClientFactory mMetadataClientFactory;
        private readonly MetadataRules mMetadataRules;
        private readonly IServiceProvider mService;

        public IRetrieveWorker CreateRetrieveWorker(IOrgAccessProvider orgAccessProvider)
        {
            var client = mMetadataClientFactory.CreateClient(orgAccessProvider);
            var logger = mService.GetRequiredService<ILogger<RetrieveWorker>>();
            var worker = new RetrieveWorker(client, mMetadataRules, logger);
            return worker;
        }
    }
}
