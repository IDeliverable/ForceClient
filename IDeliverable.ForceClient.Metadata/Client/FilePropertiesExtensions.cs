using System;
using IDeliverable.ForceClient.Metadata.ForceMetadata;

namespace IDeliverable.ForceClient.Metadata.Client
{
	static class FilePropertiesExtensions
	{
		public static bool IsInPackage(this FileProperties fileProperties)
		{
			return fileProperties.manageableState != ManageableState.unmanaged || !String.IsNullOrEmpty(fileProperties.namespacePrefix);
		}

		public static MetadataItemInfo AsMetadataItemInfo(this FileProperties fileProperties)
		{
			return new MetadataItemInfo(
				fileProperties.id,
				fileProperties.fullName,
				fileProperties.type,
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
