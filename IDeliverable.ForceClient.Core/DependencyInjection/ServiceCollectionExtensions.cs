using System;
using IDeliverable.ForceClient.Core.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddForceClient(this IServiceCollection services, Action<ForceClientConfigurationBuilder> configure = null)
		{
			var builder = new ForceClientConfigurationBuilder(services);

			configure?.Invoke(builder);

			if (!builder.RequiredDependenciesAreRegistered)
				throw new Exception($"Some required service registrations are missing. Ensure that the application calls at least one of the Use* methods on {nameof(ForceClientConfigurationBuilder)} as part of service registration.");

			return services;
		}
	}
}
