using System;
using System.Xml.Linq;
using IDeliverable.ForceClient.Metadata.Describe;

namespace IDeliverable.ForceClient.Metadata.Archives
{
	public class NestedComponent : IEquatable<NestedComponent>
	{
		internal NestedComponent(NestedMetadataTypeDescription typeDescription, string name, XElement xml)
		{
			TypeDescription = typeDescription;
			Name = name;
			Xml = xml;
		}

		public NestedMetadataTypeDescription TypeDescription { get; }
		public string Type => TypeDescription.Name;
		public string Name { get; }
		public XElement Xml { get; }

		public bool Equals(NestedComponent other)
		{
			if (other == null)
				return false;
			return Type == other.Type && Name == other.Name;
		}

		public override bool Equals(object other)
		{
			return Equals(other as NestedComponent);
		}

		public override int GetHashCode()
		{
			return (Type, Name).GetHashCode();
		}
	}
}
