using IDeliverable.ForceClient.Metadata;
using IDeliverable.ForceClient.Metadata.Client;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMetadataServices(this IServiceCollection services)
        {
            return
                services
                    .AddSingleton<MetadataRules>()
                    .AddSingleton<IMetadataClientFactory, MetadataClientFactory>();
        }
    }
}
