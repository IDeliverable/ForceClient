using System;

namespace IDeliverable.ForceClient.Metadata.Retrieve
{
    public class RetrieveException : Exception
    {
        public RetrieveException(string apiErrorMessage, string apiErrorCode)
            : base($"An error occurred during metadata retrieval. API error message: '{apiErrorMessage}'. API error code: {apiErrorCode}")
        {
            ApiErrorMessage = apiErrorMessage;
            ApiErrorCode = apiErrorCode;
        }

        public string ApiErrorMessage { get; }
        public string ApiErrorCode { get; }
    }
}
