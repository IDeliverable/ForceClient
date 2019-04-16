using System;
using IDeliverable.ForceClient.Core;
using IDeliverable.ForceClient.Metadata.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IDeliverable.ForceClient.Metadata.Processes.Retrieve
{
    public class RetrieveProcessFactory : IRetrieveProcessFactory
    {
        public RetrieveProcessFactory(IMetadataClientFactory metadataClientFactory, MetadataRules metadataRules, IServiceProvider serviceProvider)
        {
            mMetadataClientFactory = metadataClientFactory;
            mMetadataRules = metadataRules;
            mService = serviceProvider;
        }

        private readonly IMetadataClientFactory mMetadataClientFactory;
        private readonly MetadataRules mMetadataRules;
        private readonly IServiceProvider mService;

        public IRetrieveProcess CreateRetrieveProcess(IOrgAccessProvider orgAccessProvider)
        {
            var client = mMetadataClientFactory.CreateClient(orgAccessProvider);
            var logger = mService.GetRequiredService<ILogger<RetrieveProcess>>();
            var worker = new RetrieveProcess(client, mMetadataRules, logger);
            return worker;
        }
    }
}
