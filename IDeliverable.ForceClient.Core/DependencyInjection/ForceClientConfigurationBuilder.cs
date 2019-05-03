using System;
using System.Linq;
using IDeliverable.ForceClient.Core.OrgAccess;
using IDeliverable.ForceClient.Core.OrgAccess.Native;
using IDeliverable.ForceClient.Core.Tokens;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IDeliverable.ForceClient.Core.DependencyInjection
{
	public class ForceClientConfigurationBuilder
	{
		internal ForceClientConfigurationBuilder(IServiceCollection services)
		{
			mServices = services;
		}

		private readonly IServiceCollection mServices;

		internal bool RequiredDependenciesAreRegistered => mServices.Any(x => x.ServiceType == typeof(ITokenStore)) && mServices.Any(x => x.ServiceType == typeof(IOrgAccessProvider));

		public ForceClientConfigurationBuilder UseNativeClientAuthentication(string clientId, int listenerTcpPort)
		{
			// In case different parts of application calls this and/or related methods multiple
			// times, remove the relevant service registrations.
			foreach (var descriptor in mServices.Where(x => x.ServiceType == typeof(ITokenStore) || x.ServiceType == typeof(IOrgAccessProvider)).ToArray())
				mServices.Remove(descriptor);

			mServices
				.AddSingleton<ITokenStore, IsolatedTokenStore>()
				.AddSingleton<IOrgAccessProvider>(serviceProvider =>
				{
					var tokenStore = serviceProvider.GetRequiredService<ITokenStore>();
					var logger = serviceProvider.GetRequiredService<ILogger<NativeClientOrgAccessProvider>>();
					return new NativeClientOrgAccessProvider(tokenStore, logger, clientId, listenerTcpPort);
				});

			return this;
		}
	}
}
