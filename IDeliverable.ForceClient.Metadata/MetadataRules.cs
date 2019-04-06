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
    }
}
