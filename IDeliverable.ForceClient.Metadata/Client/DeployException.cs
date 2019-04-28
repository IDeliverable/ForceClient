using System;

namespace IDeliverable.ForceClient.Metadata.Client
{
	public class DeployException : Exception
	{
		public DeployException(string apiErrorMessage, string apiErrorCode)
			: base($"An error occurred during metadata deployment. API error message: '{apiErrorMessage}'. API error code: {apiErrorCode}")
		{
			ApiErrorMessage = apiErrorMessage;
			ApiErrorCode = apiErrorCode;
		}

		public string ApiErrorMessage { get; }
		public string ApiErrorCode { get; }
	}
}
