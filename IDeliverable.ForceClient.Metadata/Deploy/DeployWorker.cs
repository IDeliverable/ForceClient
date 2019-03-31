using System;
using System.Diagnostics;
using System.Threading.Tasks;
using IDeliverable.ForceClient.Metadata.Client;

namespace IDeliverable.ForceClient.Metadata.Deploy
{
    public class DeployWorker : IDeployWorker
    {
        public DeployWorker(IMetadataClient client)
        {
            mClient = client;
        }

        private readonly IMetadataClient mClient;

        public async Task<DeployResult> DeployAsync(byte[] zipFile)
        {
            var operationId = await mClient.StartDeployAsync(zipFile);

            DeployResult result = null;
            while (!(result = await mClient.GetDeployResultAsync(operationId)).IsDone)
            {
                Debug.WriteLine($"Deploy status: {result.Status} ({result.State}).");
                await Task.Delay(TimeSpan.FromSeconds(3));
            }

            return result;
        }
    }
}
