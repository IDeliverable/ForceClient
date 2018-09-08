using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using IDeliverable.Utils.Core.EventExtensions;

namespace IDeliverable.ForceClient.Metadata.Deploy
{
    public class DeployWorker : INotifyPropertyChanged
    {
        public DeployWorker(MetadataGateway gateway)
        {
            mGateway = gateway;
        }

        private MetadataGateway mGateway;

        public event PropertyChangedEventHandler PropertyChanged;

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

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.SafeRaise(ExceptionHandlingMode.Swallow, null, this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
