using System.IO;
using System.IO.Compression;

namespace IDeliverable.ForceClient.Metadata
{
    public class MetadataFile
    {
        public static MetadataFile FromZipArchiveEntry(ZipArchiveEntry entry)
        {
            using (var targetStream = new MemoryStream())
            {
                using (var entryStream = entry.Open())
                    entryStream.CopyTo(targetStream);

                return new MetadataFile(entry.FullName, targetStream.ToArray());
            }
        }

        public MetadataFile(string fullName, byte[] contents)
        {
            FullName = fullName;
            Contents = contents;
        }

        public string FullName { get; }
        public byte[] Contents { get; }
    }
}
