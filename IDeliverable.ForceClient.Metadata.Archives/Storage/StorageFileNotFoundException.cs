using System;

namespace IDeliverable.ForceClient.Metadata.Archives.Storage
{
    /// <summary>
    /// Represents the failure of an <see cref="IArchiveStorage"/> operation because a file could not be found.
    /// </summary>
    public class StorageFileNotFoundException : Exception
    {
        public StorageFileNotFoundException(string filePath)
            : base($"The file '{filePath}' does not exist.")
        {
            FilePath = filePath;
        }

        public string FilePath { get; }
    }
}
