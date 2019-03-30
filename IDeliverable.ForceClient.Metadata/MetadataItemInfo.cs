namespace IDeliverable.ForceClient.Metadata
{
    public class MetadataItemInfo
    {
        public MetadataItemInfo(MetadataType type, string fullName)
        {
            Type = type;
            FullName = fullName;
        }

        public MetadataType Type { get; }
        public string FullName { get; }
    }
}
