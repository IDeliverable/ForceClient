using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace IDeliverable.ForceClient.Core.ServiceModel
{
    class OrgAccessInspector : IClientMessageInspector
    {
        public OrgAccessInspector(IOrgAccessProvider orgAccessProvider)
        {
            mOrgAccessProvider = orgAccessProvider;
        }

        private readonly IOrgAccessProvider mOrgAccessProvider;

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            var accessToken = mOrgAccessProvider.GetAccessTokenAsync().Result;

            request.Headers.Add(new SessionHeader(accessToken));

            return Guid.NewGuid();
        }
    }
}
