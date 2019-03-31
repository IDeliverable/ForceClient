using IDeliverable.ForceClient.Metadata;
using IDeliverable.ForceClient.Metadata.Client;
using IDeliverable.ForceClient.Metadata.Deploy;
using IDeliverable.ForceClient.Metadata.Retrieve;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMetadataServices(this IServiceCollection services)
        {
            return
                services
                    .AddSingleton<MetadataRules>()
                    .AddSingleton<IMetadataClientFactory, MetadataClientFactory>()
                    .AddSingleton<IDeployWorkerFactory, DeployWorkerFactory>()
                    .AddSingleton<IRetrieveWorkerFactory, RetrieveWorkerFactory>();
        }
    }
}
