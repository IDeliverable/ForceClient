using System;

namespace IDeliverable.ForceClient.Metadata
{
	public class MetadataItemInfo : IEquatable<MetadataItemInfo>
	{
		public MetadataItemInfo(string id, string name, string type, string filePath, string createdById, string createdByName, DateTime createdUtc, string lastModifiedById, string lastModifiedByName, DateTime lastModifiedUtc, bool isInPackage, string namespacePrefix)
		{
			Id = id;
			Name = name;
			Type = type;
			FilePath = filePath;
			CreatedById = createdById;
			CreatedByName = createdByName;
			CreatedUtc = createdUtc;
			LastModifiedById = lastModifiedById;
			LastModifiedByName = lastModifiedByName;
			LastModifiedUtc = lastModifiedUtc;
			IsInPackage = isInPackage;
			NamespacePrefix = namespacePrefix;
		}

		public string Id { get; }
		public string Name { get; }
		public string Type { get; }
		public string FilePath { get; }
		public string CreatedById { get; }
		public string CreatedByName { get; }
		public DateTime CreatedUtc { get; }
		public string LastModifiedById { get; }
		public string LastModifiedByName { get; }
		public DateTime LastModifiedUtc { get; }
		public bool IsInPackage { get; }
		public string NamespacePrefix { get; }

		public bool Equals(MetadataItemInfo other)
		{
			if (other == null)
				return false;
			return Type == other.Type && Name == other.Name && Id == other.Id;
		}

		public override bool Equals(object other)
		{
			return Equals(other as MetadataItemInfo);
		}

		public override int GetHashCode()
		{
			return (Type, Name, Id).GetHashCode();
		}
	}
}
