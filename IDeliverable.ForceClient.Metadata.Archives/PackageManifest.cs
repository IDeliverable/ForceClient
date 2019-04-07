using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using IDeliverable.ForceClient.Metadata.Archives.Storage;

namespace IDeliverable.ForceClient.Metadata.Archives
{
    public class PackageManifest
    {
        private static readonly XNamespace mNamespace = XNamespace.Get("http://soap.sforce.com/2006/04/metadata");

        public static async Task<PackageManifest> LoadFromAsync(IArchiveStorage storage, string filePath)
        {
            XDocument doc = null;
            using (var readStream = await storage.OpenReadAsync(filePath))
                doc = await XDocument.LoadAsync(readStream, LoadOptions.None, CancellationToken.None);
            return new PackageManifest(doc);
        }

        public PackageManifest()
        {
            Components = new HashSet<(string, string)>();
        }

        public PackageManifest(XDocument doc)
        {
            var rootElement = doc.Root;
            var namespaceName = rootElement.Name.NamespaceName;

            if (namespaceName != mNamespace)
                throw new ArgumentException($"The element '{rootElement.Name}' is not supported.");

            // Only present for managed packages.
            Name = rootElement.Element(XName.Get("fullName", namespaceName))?.Value;
            var apiAccessLevelString = rootElement.Element(XName.Get("apiAccessLevel", namespaceName))?.Value;
            if (apiAccessLevelString != null)
                ApiAccessLevel = (ApiAccessLevel)Enum.Parse(typeof(ApiAccessLevel), apiAccessLevelString);
            Description = rootElement.Element(XName.Get("description", namespaceName))?.Value;
            NamespacePrefix = rootElement.Element(XName.Get("namespacePrefix", namespaceName))?.Value;
            SetupWeblink = rootElement.Element(XName.Get("setupWeblink", namespaceName))?.Value;

            Components = new HashSet<(string, string)>();
            foreach (var typeElement in rootElement.Elements(XName.Get("types", namespaceName)))
            {
                var type = typeElement.Element(XName.Get("name", namespaceName)).Value;
                foreach (var memberElement in typeElement.Elements(XName.Get("members", namespaceName)))
                    Components.Add((type, memberElement.Value));
            }

            Version = Double.Parse(rootElement.Element(XName.Get("version", namespaceName)).Value, CultureInfo.InvariantCulture);
        }

        public string Name { get; }
        public ApiAccessLevel? ApiAccessLevel { get; }
        public string Description { get; }
        public string NamespacePrefix { get; }
        public string SetupWeblink { get; }
        public ISet<(string type, string name)> Components { get; }
        public double Version { get; }

        public async Task SaveToAsync(IArchiveStorage storage, string filePath)
        {
            var elements = new List<XElement>();

            // Only present for managed packages.
            if (!String.IsNullOrEmpty(Name))
                elements.Add(new XElement(mNamespace + "fullName", Name));
            if (ApiAccessLevel.HasValue)
                elements.Add(new XElement(mNamespace + "apiAccessLevel", ApiAccessLevel.Value.ToString()));
            if (!String.IsNullOrEmpty(Description))
                elements.Add(new XElement(mNamespace + "description", Description));
            if (!String.IsNullOrEmpty(NamespacePrefix))
                elements.Add(new XElement(mNamespace + "namespacePrefix", NamespacePrefix));
            if (!String.IsNullOrEmpty(SetupWeblink))
                elements.Add(new XElement(mNamespace + "setupWeblink", SetupWeblink));

            var componentElements =
                from component in Components
                group component.name by component.type into typeGroup
                orderby typeGroup.Key
                select new XElement(mNamespace + "types",
                    from member in typeGroup
                    orderby member
                    select new XElement(mNamespace + "members", member)
                );
            elements.AddRange(componentElements);

            elements.Add(new XElement(mNamespace + "version", Version.ToString(CultureInfo.InvariantCulture)));

            var rootElement = new XElement(mNamespace + "Package", elements);
            var doc = new XDocument(rootElement);

            using (var writeStream = await storage.OpenWriteAsync(filePath))
            {
                var settings = new XmlWriterSettings()
                {
                    IndentChars = "  ",
                    Indent = true,
                    Encoding = Encoding.UTF8
                };

                using (var writer = XmlWriter.Create(writeStream, settings))
                    doc.Save(writer);
            }
        }
    }
}
