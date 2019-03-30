using System;
using System.Diagnostics;
using System.Threading.Tasks;
using IDeliverable.ForceClient.Metadata.Client;

namespace IDeliverable.ForceClient.Metadata.Deploy
{
    public class DeployWorker
    {
        public DeployWorker(IMetadataClient gateway)
        {
            mGateway = gateway;
        }

        private readonly IMetadataClient mGateway;

        public async Task<DeployResult> DeployAsync(byte[] zipFile)
        {
            var operationId = await mGateway.StartDeployAsync(zipFile);

            DeployResult result = null;
            while (!(result = await mGateway.GetDeployResultAsync(operationId)).IsDone)
            {
                Debug.WriteLine($"Deploy status: {result.Status} ({result.State}).");
                await Task.Delay(TimeSpan.FromSeconds(3));
            }

            return result;
        }
    }
}
