using System;
using IDeliverable.ForceClient.Metadata.ForceMetadata;

namespace IDeliverable.ForceClient.Metadata.Client
{
	static class FilePropertiesExtensions
	{
		public static bool IsInPackage(this FileProperties fileProperties)
		{
			// INFO: Not sure which logic would be correct here given what we are trying to do. If we only include "unmanaged"
			// components then things like standard objects (which can be customized) will not be included because those are in state
			// "released".
			//return fileProperties.manageableState != ManageableState.unmanaged || !String.IsNullOrEmpty(fileProperties.namespacePrefix);
			return !String.IsNullOrEmpty(fileProperties.namespacePrefix);
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
				fileProperties.namespacePrefix == "" ? null : fileProperties.namespacePrefix);
		}
	}
}
