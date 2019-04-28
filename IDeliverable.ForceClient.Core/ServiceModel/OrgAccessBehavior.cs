using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using IDeliverable.ForceClient.Core.OrgAccess;

namespace IDeliverable.ForceClient.Core.ServiceModel
{
	class OrgAccessBehavior : IEndpointBehavior
	{
		public OrgAccessBehavior(IOrgAccessProvider orgAccessProvider)
		{
			mOrgAccessProvider = orgAccessProvider;
		}

		private readonly IOrgAccessProvider mOrgAccessProvider;

		public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
		{
		}

		public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
		{
			clientRuntime.ClientMessageInspectors.Add(new OrgAccessInspector(mOrgAccessProvider));
		}

		public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
		{
		}

		public void Validate(ServiceEndpoint endpoint)
		{
		}
	}
}
