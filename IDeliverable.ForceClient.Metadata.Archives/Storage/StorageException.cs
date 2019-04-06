using System;

namespace IDeliverable.ForceClient.Metadata.Archives.Storage
{
    /// <summary>
    /// Represents the failure of an <see cref="IArchiveStorage"/> operation.
    /// </summary>
    public class StorageException : Exception
    {
        public StorageException(string message)
            : base(message)
        {
        }

        public StorageException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
