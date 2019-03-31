using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IDeliverable.ForceClient.Metadata.Client
{
    public class MetadataClientFactory : IMetadataClientFactory
    {
        public MetadataClientFactory(MetadataRules metadataRules, IServiceProvider services)
        {
            mMetadataRules = metadataRules;
            mServices = services;
        }

        private readonly MetadataRules mMetadataRules;
        private readonly IServiceProvider mServices;

        public IMetadataClient CreateClient(IOrgAccessProvider orgAccessProvider)
        {
            var logger = mServices.GetRequiredService<ILogger<SoapMetadataClient>>();
            return new SoapMetadataClient(orgAccessProvider, mMetadataRules, logger);
        }
    }
}
