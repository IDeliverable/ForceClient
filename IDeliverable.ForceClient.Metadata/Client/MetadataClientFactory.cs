using System;
using IDeliverable.ForceClient.Core.OrgAccess;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IDeliverable.ForceClient.Metadata.Client
{
	public class MetadataClientFactory : IMetadataClientFactory
	{
		public MetadataClientFactory(IOrgAccessProvider orgAccessProvider, MetadataRules metadataRules, IServiceProvider services)
		{
			mOrgAccessProvider = orgAccessProvider;
			mMetadataRules = metadataRules;
			mServices = services;
		}

		private readonly IOrgAccessProvider mOrgAccessProvider;
		private readonly MetadataRules mMetadataRules;
		private readonly IServiceProvider mServices;

		public IMetadataClient CreateClient(OrgType orgType, string username)
		{
			// INFO: Right now we only have one IMetadataClient implementation (for SOAP) but this
			// will probably changed in the future.
			var logger = mServices.GetRequiredService<ILogger<SoapMetadataClient>>();
			return new SoapMetadataClient(mOrgAccessProvider, mMetadataRules, logger, orgType, username);
		}
	}
}
