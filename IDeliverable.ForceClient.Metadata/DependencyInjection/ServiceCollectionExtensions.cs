using System;
using IDeliverable.ForceClient.Core.DependencyInjection;
using IDeliverable.ForceClient.Metadata;
using IDeliverable.ForceClient.Metadata.Client;

namespace Microsoft.Extensions.DependencyInjection
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddForceMetadataClient(this IServiceCollection services, Action<ForceClientConfigurationBuilder> configure = null)
		{
			return
				services
					.AddForceClient(configure)
					.AddSingleton<MetadataRules>()
					.AddSingleton<IMetadataClientFactory, MetadataClientFactory>();
		}
	}
}
