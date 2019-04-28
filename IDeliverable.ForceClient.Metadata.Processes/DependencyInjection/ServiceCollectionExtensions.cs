using IDeliverable.ForceClient.Metadata.Processes.Deploy;
using IDeliverable.ForceClient.Metadata.Processes.Retrieve;

namespace Microsoft.Extensions.DependencyInjection
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddMetadataProcesses(this IServiceCollection services)
		{
			return
				services
					.AddMetadataServices()
					.AddSingleton<IDeployProcessFactory, DeployProcessFactory>()
					.AddSingleton<IRetrieveProcessFactory, RetrieveProcessFactory>();
		}
	}
}
