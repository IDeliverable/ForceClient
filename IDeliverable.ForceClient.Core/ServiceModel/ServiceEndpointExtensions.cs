using System;
using System.Globalization;
using IDeliverable.ForceClient.Core.OrgAccess;
using IDeliverable.ForceClient.Core.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel;

namespace IDeliverable.ForceClient.Core.ServiceModel
{
	public static class ServiceEndpointExtensions
	{
		public static void ConfigureOrgAccess(this ServiceEndpoint endpoint, IOrgAccessProvider orgAccessProvider, string apiName, int apiVersion)
		{
			var soapUrl = orgAccessProvider.GetSoapApiUrlAsync(apiName).Result;
			var endpointAddress = new EndpointAddress(new Uri(soapUrl.Replace("{version}", apiVersion.ToString(CultureInfo.InvariantCulture))));
			endpoint.Address = endpointAddress;

			endpoint.EndpointBehaviors.Add(new OrgAccessBehavior(orgAccessProvider));
		}
	}
}
