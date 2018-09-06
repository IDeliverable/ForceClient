using System.ServiceModel.Description;

namespace IDeliverable.ForceClient.Core
{
    public static class SessionHeaderExtensions
    {
        public static void SetSessionId(this ServiceEndpoint endpoint, string sessionId)
        {
            endpoint.EndpointBehaviors.Add(new SessionHeaderBehavior(sessionId));
        }
    }
}
