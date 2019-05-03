using IDeliverable.ForceClient.Metadata.Processes;

namespace Microsoft.Extensions.DependencyInjection
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddForceMetadataProcesses(this IServiceCollection services)
		{
			return
				services
					.AddForceMetadataClient()
					.AddSingleton<DeployProcess>()
					.AddSingleton<RetrieveProcess>();
		}
	}
}
