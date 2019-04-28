using System.Collections.Generic;

namespace IDeliverable.ForceClient.Metadata.Describe
{
	public class MetadataDescription
	{
		internal MetadataDescription(string orgNamespace, bool partialSaveIsAllowed, bool testIsRequired, IReadOnlyDictionary<string, MetadataTypeDescription> types)
		{
			OrgNamespace = orgNamespace;
			PartialSaveIsAllowed = partialSaveIsAllowed;
			TestIsRequired = testIsRequired;
			Types = types;
		}

		/// <summary>
		/// The namespace of the organization. Specify only for Developer Edition organizations that can contain a managed package. The managed package has a namespace specified when it is created.
		/// </summary>
		public string OrgNamespace { get; }

		/// <summary>
		/// Indicates whether it is allowed (true) or not (false) to specify <c>rollbackOnError</c> as <c>false</c> when deploying to this org. Always <c>false</c> for production orgs.
		/// </summary>
		public bool PartialSaveIsAllowed { get; }

		/// <summary>
		/// Indicates whether it is required (true) or not (false) to run tests when deploying to this org. Always <c>true</c> for production orgs.
		/// </summary>
		public bool TestIsRequired { get; }

		/// <summary>
		/// A list of metadata types supported by this org.
		/// </summary>
		public IReadOnlyDictionary<string, MetadataTypeDescription> Types { get; }
	}
}
