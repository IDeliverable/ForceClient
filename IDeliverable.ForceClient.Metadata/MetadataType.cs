using System.Linq;

namespace IDeliverable.ForceClient.Metadata
{
    public enum MetadataType
    {
        CustomObject,
        CustomField,
		Dashboard,
		Document,
		EmailTemplate,
		Report
    }

	public static class MetadataTypeExtensions
	{
		public static bool GetIsInFolders(this MetadataType metadataType)
		{
			var metadataTypesInFolders = new[] { MetadataType.Dashboard, MetadataType.Document, MetadataType.EmailTemplate, MetadataType.Report };
			return metadataTypesInFolders.Contains(metadataType);
		}
	}
}
