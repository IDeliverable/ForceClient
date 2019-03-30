namespace IDeliverable.ForceClient.Metadata.Client
{
    public class MetadataListQuery
    {
        public MetadataListQuery(MetadataType type, string inFolder = null)
        {
            Type = type;
            InFolder = inFolder;
        }

        public MetadataType Type { get; }
        public string InFolder { get; }
    }
}
