using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace IDeliverable.ForceClient.Core
{
    internal class SessionHeaderBehavior : IEndpointBehavior
    {
        public SessionHeaderBehavior(string sessionId)
        {
            mSessionId = sessionId;
        }

        private readonly string mSessionId;

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
            
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.ClientMessageInspectors.Add(new SessionHeaderInspector(mSessionId));
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            
        }

        public void Validate(ServiceEndpoint endpoint)
        {
            
        }
    }
}
