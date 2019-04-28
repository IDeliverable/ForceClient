namespace IDeliverable.ForceClient.Metadata
{
	public class MetadataListQuery
	{
		public MetadataListQuery(string type, string inFolder = null)
		{
			Type = type;
			InFolder = inFolder;
		}

		public string Type { get; }
		public string InFolder { get; }
	}
}
