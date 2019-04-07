using System.Collections.Generic;
using System.Linq;

namespace IDeliverable.ForceClient.Metadata.Describe
{
    public class MetadataTypeDescription
    {
        internal MetadataTypeDescription(string name, string archiveDirectoryName, string archiveFileNameExtension, bool isFolderized, bool hasMetaFile, IEnumerable<NestedMetadataTypeDescription> nestedTypes)
        {
            Name = name;
            ArchiveDirectoryName = archiveDirectoryName;
            ArchiveFileNameExtension = archiveFileNameExtension;
            IsFolderized = isFolderized;
            HasMetaFile = hasMetaFile;
            NestedTypes = nestedTypes;
        }

        /// <summary>
        /// The name of the root element in a metadata file for a component of this type. This name also appears in the types/name element in the package manifest file.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The name of the directory (in a metadata package) that contains components of this type.
        /// </summary>
        public string ArchiveDirectoryName { get; }

        /// <summary>
        /// The file name extension for this components of this type in a metadata package.
        /// </summary>
        public string ArchiveFileNameExtension { get; }

        /// <summary>
        /// Indicates whether components of this type are organized in folders (true) or not (false). For example, documents, email templates and reports are stored in folders.
        /// </summary>
        public bool IsFolderized { get; }

        /// <summary>
        /// Indicates whether components of this type require an accompanying metadata file (*-meta.xml). For example, documents, classes, and s-controls are components that require an additional metadata file.
        /// </summary>
        public bool HasMetaFile { get; }

        /// <summary>
        /// List of metadata types whose components are nested within components of this type.
        /// </summary>
        public IEnumerable<NestedMetadataTypeDescription> NestedTypes { get; }

        /// <summary>
        /// Indicates whether this type has any nested types.
        /// </summary>
        public bool HasNestedTypes => NestedTypes != null && NestedTypes.Any();
    }
}
