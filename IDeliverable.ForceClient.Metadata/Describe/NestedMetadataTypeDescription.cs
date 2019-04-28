namespace IDeliverable.ForceClient.Metadata.Describe
{
	public class NestedMetadataTypeDescription
	{
		internal NestedMetadataTypeDescription(string name, string elementName, string keyChildElementName)
		{
			Name = name;
			ElementName = elementName;
			KeyChildElementName = keyChildElementName;
		}

		/// <summary>
		/// The name of this nested type as it appears in the types/name element in the package manifest file.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// The name of the XML element in the parent metadata file for a component of this nested type.
		/// </summary>
		public string ElementName { get; }

		/// <summary>
		/// The name of the XML child element in a component of this nested type which contains the component's unique name.
		/// </summary>
		public string KeyChildElementName { get; }
	}
}
