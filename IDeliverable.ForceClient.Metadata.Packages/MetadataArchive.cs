using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using IDeliverable.ForceClient.Metadata.Components;

namespace IDeliverable.ForceClient.Metadata
{
    public class MetadataArchive
    {
        public static MetadataArchive FromStream(Stream zipStream)
        {
            using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                var packagesQuery =
                    from entry in zip.Entries
                    let packageName = entry.FullName.Split('\\', '/').First() // First folder level is always the package.
                    let file = MetadataFile.FromZipArchiveEntry(entry)
                    group file by packageName into packageFiles
                    select MetadataPackage.FromFiles(packageFiles.Key, packageFiles);

                return new MetadataArchive(packagesQuery.ToArray());
            }
        }

        public static MetadataArchive FromZipFile(byte[] zipFile)
        {
            using (var zipStream = new MemoryStream(zipFile))
                return FromStream(zipStream);
        }

        public MetadataArchive(IEnumerable<MetadataPackage> packages)
        {
            Packages = packages;
        }

        public IEnumerable<MetadataPackage> Packages { get; }

        public IEnumerable<CustomObject> CustomObjects
        {
            get
            {
                var customObjectsQuery =
                    from p in Packages
                    from o in p.CustomObjects
                    select o;

                return customObjectsQuery.ToArray();
            }
        }

        public void WriteTo(Stream zipStream)
        {
            using (var targetZip = new ZipArchive(zipStream, ZipArchiveMode.Create))
            {
                foreach (var package in Packages)
                {
                    foreach (var file in package.ToFiles())
                    {
                        var targetEntry = targetZip.CreateEntry(file.FullName);
                        using (var targetEntryStream = targetEntry.Open())
                            targetEntryStream.Write(file.Contents, 0, file.Contents.Length);
                    }
                }
            }
        }

        public byte[] ToZipFile()
        {
            using (var targetStream = new MemoryStream())
            {
                WriteTo(targetStream);
                return targetStream.ToArray();
            }
        }

        public MetadataArchive SelectChanges()
        {
            var changedPackagesQuery =
                from package in Packages
                let changes = package.SelectChanges()
                where changes != null
                select changes;

            if (changedPackagesQuery.Any())
                return new MetadataArchive(changedPackagesQuery.ToArray());

            return null;
        }
    }
}
