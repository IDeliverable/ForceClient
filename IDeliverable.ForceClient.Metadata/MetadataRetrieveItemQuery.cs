namespace IDeliverable.ForceClient.Metadata
{
	public class MetadataRetrieveItemQuery
	{
		public MetadataRetrieveItemQuery(string type, string name)
		{
			Type = type;
			Name = name;
		}

		public string Type { get; }
		public string Name { get; }
	}
}
