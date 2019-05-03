using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using IDeliverable.ForceClient.Core.OrgAccess;

namespace IDeliverable.ForceClient.Core.ServiceModel
{
	class OrgAccessBehavior : IEndpointBehavior
	{
		public OrgAccessBehavior(IOrgAccessProvider orgAccessProvider, OrgType orgType, string username)
		{
			mOrgAccessProvider = orgAccessProvider;
			mOrgType = orgType;
			mUsername = username;
		}

		private readonly IOrgAccessProvider mOrgAccessProvider;
		private readonly OrgType mOrgType;
		private readonly string mUsername;

		public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
		{
		}

		public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
		{
			clientRuntime.ClientMessageInspectors.Add(new OrgAccessInspector(mOrgAccessProvider, mOrgType, mUsername));
		}

		public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
		{
		}

		public void Validate(ServiceEndpoint endpoint)
		{
		}
	}
}
