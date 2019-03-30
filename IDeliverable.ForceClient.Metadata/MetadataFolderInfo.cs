namespace IDeliverable.ForceClient.Metadata
{
    public class MetadataFolderInfo
    {
        public MetadataFolderInfo(string name, MetadataType containsType)
        {
            Name = name;
            ContainsType = containsType;
        }

        public string Name { get; }
        public MetadataType ContainsType { get; }
    }
}
