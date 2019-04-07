using System.Collections.Generic;
using System.Xml.Linq;

namespace IDeliverable.ForceClient.Metadata.Archives
{
    public class NestedComponent
    {
        public NestedComponent(string type, string name, XElement xml)
        {
            Type = type;
            Name = name;
            Xml = xml;
        }

        public string Type { get; }
        public string Name { get; }
        public XElement Xml { get; }

        public class EqualityComparer : IEqualityComparer<NestedComponent>
        {
            public bool Equals(NestedComponent x, NestedComponent y)
            {
                return x.Type == y.Type && x.Name == y.Name;
            }

            public int GetHashCode(NestedComponent obj)
            {
                unchecked
                {
                    var hashCode = 47;
                    if (obj.Type != null)
                        hashCode = (hashCode * 53) ^ EqualityComparer<string>.Default.GetHashCode(obj.Type);
                    if (obj.Name != null)
                        hashCode = (hashCode * 53) ^ EqualityComparer<string>.Default.GetHashCode(obj.Name);
                    if (obj.Xml != null)
                        hashCode = (hashCode * 53) ^ EqualityComparer<XElement>.Default.GetHashCode(obj.Xml);
                    return hashCode;
                }
            }
        }
    }
}
