using System;
using System.Globalization;
using System.ServiceModel;
using System.ServiceModel.Description;
using IDeliverable.ForceClient.Core.OrgAccess;

namespace IDeliverable.ForceClient.Core.ServiceModel
{
	public static class ServiceEndpointExtensions
	{
		public static void ConfigureOrgAccess(this ServiceEndpoint endpoint, IOrgAccessProvider orgAccessProvider, OrgType orgType, string username, string apiName, int apiVersion)
		{
			var soapUrl = orgAccessProvider.GetSoapApiUrlAsync(orgType, username, apiName).Result;
			var endpointAddress = new EndpointAddress(new Uri(soapUrl.Replace("{version}", apiVersion.ToString(CultureInfo.InvariantCulture))));
			endpoint.Address = endpointAddress;

			endpoint.EndpointBehaviors.Add(new OrgAccessBehavior(orgAccessProvider, orgType, username));
		}
	}
}
