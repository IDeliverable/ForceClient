namespace IDeliverable.ForceClient.Metadata
{
    public class MetadataItemReference
    {
        public MetadataItemReference(MetadataType type, string fullName)
        {
            Type = type;
            FullName = fullName;
        }

        public MetadataType Type { get; }
        public string FullName { get; }
    }
}
