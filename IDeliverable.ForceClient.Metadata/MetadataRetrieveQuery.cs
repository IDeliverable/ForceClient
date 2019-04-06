namespace IDeliverable.ForceClient.Metadata
{
    public class MetadataRetrieveQuery
    {
        public MetadataRetrieveQuery(string type, string name)
        {
            Name = name;
            Type = type;
        }

        public string Type { get; }
        public string Name { get; }
    }
}
