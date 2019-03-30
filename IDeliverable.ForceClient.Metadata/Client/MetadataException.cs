using System;

namespace IDeliverable.ForceClient.Metadata.Retrieve
{
    public class MetadataException : Exception
    {
        public MetadataException(string apiErrorMessage, string apiErrorCode)
            : this(apiErrorMessage, apiErrorCode, innerException: null)
        {
        }

        public MetadataException(string apiErrorMessage, string apiErrorCode, Exception innerException)
            : base($"An error occurred during metadata operation. API error message: '{apiErrorMessage}'. API error code: {apiErrorCode}", innerException)
        {
            ApiErrorMessage = apiErrorMessage;
            ApiErrorCode = apiErrorCode;
        }

        public string ApiErrorMessage { get; }
        public string ApiErrorCode { get; }
    }
}
