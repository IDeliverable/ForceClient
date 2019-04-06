using System;

namespace IDeliverable.ForceClient.Metadata.Archives.Storage
{
    /// <summary>
    /// Contains basic information about a file managed by an <see cref="IArchiveStorage"/> instance.
    /// </summary>
    public class FileProperties
    {
        public FileProperties(string path, long size, DateTime timestampUtc)
        {
            Path = path;
            Size = size;
            TimestampUtc = timestampUtc;
        }

        /// <summary>
        /// The path of the file, relative to the root of the <see cref="IStorageProvider"/>.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// The size in bytes of the file.
        /// </summary>
        public long Size { get; }

        /// <summary>
        /// The last-modified timestamp of the file.
        /// </summary>
        public DateTime TimestampUtc { get; }
    }
}
