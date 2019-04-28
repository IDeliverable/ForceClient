namespace IDeliverable.ForceClient.Metadata
{
	public class MetadataFolderInfo
	{
		public MetadataFolderInfo(string name, string containsType)
		{
			Name = name;
			ContainsType = containsType;
		}

		public string Name { get; }
		public string ContainsType { get; }
	}
}
