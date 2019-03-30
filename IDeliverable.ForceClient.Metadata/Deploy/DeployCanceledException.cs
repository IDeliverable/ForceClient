using System;

namespace IDeliverable.ForceClient.Metadata.Deploy
{
    public class DeployCanceledException : Exception
    {
        public DeployCanceledException(string canceledByName)
            : base($"The deployment operation was canceled by user '{canceledByName}'.")
        {
            CanceledByName = canceledByName;
        }

        public string CanceledByName { get; }
    }
}
