using System;
using System.Xml.Linq;

namespace IDeliverable.ForceClient.Metadata.Archives
{
    public class NestedComponent : IEquatable<NestedComponent>
    {
        internal NestedComponent(string type, string name, XElement xml)
        {
            Type = type;
            Name = name;
            Xml = xml;
        }

        public string Type { get; }
        public string Name { get; }
        public XElement Xml { get; }

        public bool Equals(NestedComponent other)
        {
            return Type == other.Type && Name == other.Name;
        }
    }
}
