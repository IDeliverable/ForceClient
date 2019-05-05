using System;
using System.Diagnostics;
using System.Threading.Tasks;
using IDeliverable.ForceClient.Core.OrgAccess;
using IDeliverable.ForceClient.Metadata.Client;

namespace IDeliverable.ForceClient.Metadata.Processes
{
	public class DeployProcess : IDeployProcess
	{
		public DeployProcess(IMetadataClientFactory clientFactory)
		{
			mClientFactory = clientFactory;
		}

		private readonly IMetadataClientFactory mClientFactory;

		public async Task<DeployResult> DeployAsync(OrgType orgType, string username, byte[] zipFile)
		{
			var client = mClientFactory.CreateClient(orgType, username);
			var operationId = await client.StartDeployAsync(zipFile);

			DeployResult result;
			while (!(result = await client.GetDeployResultAsync(operationId)).IsDone)
			{
				Debug.WriteLine($"Deploy status: {result.Status} ({result.State}).");
				await Task.Delay(TimeSpan.FromSeconds(3));
			}

			return result;
		}
	}
}
