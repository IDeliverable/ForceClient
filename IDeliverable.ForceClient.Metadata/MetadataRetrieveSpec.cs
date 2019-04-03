namespace IDeliverable.ForceClient.Metadata
{
    public class MetadataRetrieveSpec
    {
        public MetadataRetrieveSpec(MetadataType type, string name)
        {
            Name = name;
            Type = type;
        }

        public MetadataType Type { get; }
        public string Name { get; }
    }
}
