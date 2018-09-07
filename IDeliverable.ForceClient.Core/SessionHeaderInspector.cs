using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace IDeliverable.ForceClient.Core
{
    internal class SessionHeaderInspector : IClientMessageInspector
    {
        public SessionHeaderInspector(string sessionId)
        {
            mSessionId = sessionId;
        }

        private readonly string mSessionId;

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {

        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            request.Headers.Add(new SessionHeader(mSessionId));

            return Guid.NewGuid();
        }
    }
}
