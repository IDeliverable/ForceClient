using System;
using System.Collections.Generic;
using System.Linq;

namespace IDeliverable.ForceClient.Metadata
{
    public class MetadataRules
    {
        public readonly int MetadataApiVersion = 43;

        // Limits documented here:
        // https://developer.salesforce.com/docs/atlas.en-us.salesforce_app_limits_cheatsheet.meta/salesforce_app_limits_cheatsheet/salesforce_app_limits_platform_metadata.htm
        public readonly int MaxListMetadataQueriesPerRequest = 3;
        public readonly int MaxConcurrentListMetadataRequests = 5;
        public readonly int MaxConcurrentRetrieveMetadataRequests = 5;
        public readonly int MaxConcurrentDeployMetadataRequests = 5;
        public readonly int MaxRetrieveMetadataItemsPerRequest = 10000;
        public readonly int MaxDeployMetadataItemsPerRequest = 10000;
        public readonly int MaxDeployZipFileSizeBytes = 39_000_000;

        public readonly IEnumerable<MetadataType> FolderizedTypes =
            new[]
            {
                MetadataType.Dashboard,
                MetadataType.Document,
                /*MetadataType.EmailTemplate,*/
                MetadataType.Report
            };

        public IEnumerable<MetadataType> GetSupportedTypes()
        {
            return Enum.GetValues(typeof(MetadataType)).Cast<MetadataType>().ToArray();
        }

        public bool GetIsFolderized(MetadataType metadataType)
        {
            return FolderizedTypes.Contains(metadataType);
        }
    }
}
