using System;
using IDeliverable.ForceClient.Metadata.ForceMetadata;

namespace IDeliverable.ForceClient.Metadata.Client
{
    static class FilePropertiesExtensions
    {
        public static bool IsInPackage(this FileProperties fileProperties)
        {
            return fileProperties.manageableState != ManageableState.unmanaged;
        }

        public static MetadataItemInfo AsMetadataItemInfo(this FileProperties fileProperties)
        {
            return new MetadataItemInfo(
                fileProperties.id,
                fileProperties.fullName,
                (MetadataType)Enum.Parse(typeof(MetadataType), fileProperties.type),
                fileProperties.fileName,
                fileProperties.createdById,
                fileProperties.createdByName,
                fileProperties.createdDate.ToUniversalTime(),
                fileProperties.lastModifiedById,
                fileProperties.lastModifiedByName,
                fileProperties.lastModifiedDate.ToUniversalTime(),
                fileProperties.IsInPackage(),
                fileProperties.namespacePrefix);
        }
    }
}
