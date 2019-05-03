using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using IDeliverable.ForceClient.Core.OrgAccess;

namespace IDeliverable.ForceClient.Core.ServiceModel
{
	class OrgAccessInspector : IClientMessageInspector
	{
		public OrgAccessInspector(IOrgAccessProvider orgAccessProvider, OrgType orgType, string username)
		{
			mOrgAccessProvider = orgAccessProvider;
			mOrgType = orgType;
			mUsername = username;
		}

		private readonly IOrgAccessProvider mOrgAccessProvider;
		private readonly OrgType mOrgType;
		private readonly string mUsername;

		public void AfterReceiveReply(ref Message reply, object correlationState)
		{
		}

		public object BeforeSendRequest(ref Message request, IClientChannel channel)
		{
			var accessToken = mOrgAccessProvider.GetAccessTokenAsync(mOrgType, mUsername, forceRefresh: false).Result;

			request.Headers.Add(new SessionHeader(accessToken));

			return Guid.NewGuid();
		}
	}
}
