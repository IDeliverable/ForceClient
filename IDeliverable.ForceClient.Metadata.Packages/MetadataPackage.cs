using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using IDeliverable.ForceClient.Metadata.Components;
using IDeliverable.Utils.Core;

namespace IDeliverable.ForceClient.Metadata
{
    public class MetadataPackage
    {
        public const string MetadataXmlNamespace = "http://soap.sforce.com/2006/04/metadata";

        private static readonly XName sElementName = XName.Get("Package", MetadataXmlNamespace);

        public static MetadataPackage FromFiles(string name, IEnumerable<MetadataFile> files)
        {
            var customObjectsQuery =
                from file in files
                where Path.GetExtension(file.FullName) == ".object"
                let objectName = Path.GetFileNameWithoutExtension(file.FullName)
                let doc = GetXDocumentFromBuffer(file.Contents)
                select CustomObject.FromXml(objectName, doc.Root);

            return new MetadataPackage(name, customObjectsQuery.ToArray());
        }

        public MetadataPackage(string name, IEnumerable<CustomObject> customObjects)
        {
            Name = name;
            CustomObjects = customObjects;
        }

        public string Name { get; }
        public IEnumerable<CustomObject> CustomObjects { get; }

        public IEnumerable<MetadataFile> ToFiles()
        {
            var packageManifestFile =
                new MetadataFile
                (
                    $"{Name}/package.xml",
                    new XElement
                    (
                        sElementName,
                        new XElement
                        (
                            XName.Get("types", MetadataXmlNamespace),
                            from customObject in CustomObjects
                            from customField in customObject.CustomFields
                            select new XElement(XName.Get("members", MetadataXmlNamespace), $"{customObject.Name}.{customField.Name}"),
                            new XElement(XName.Get("name", MetadataXmlNamespace), "CustomField")
                        ),
                        new XElement(XName.Get("version", MetadataXmlNamespace), "38.0") // TODO: Where to get the API version from when serializing a package manifest?
                    ).ToByteArray()
                );

            var filesQuery =
                from customObject in CustomObjects
                let fullName = $"{Name}/objects/{customObject.Name}.object"
                let contents = customObject.ToXml().ToByteArray()
                select new MetadataFile(fullName, contents);

            return filesQuery.ToArray().Union(new[] { packageManifestFile });
        }

        public MetadataPackage SelectChanges()
        {
            var changedCustomObjectsQuery =
                from customObject in CustomObjects
                let changes = customObject.SelectChanges()
                where changes != null
                select changes;

            if (changedCustomObjectsQuery.Any())
                return new MetadataPackage(Name, changedCustomObjectsQuery.ToArray());

            return null;
        }

        private static XDocument GetXDocumentFromBuffer(byte[] buffer)
        {
            using (var s = new MemoryStream(buffer))
                return XDocument.Load(s);
        }
    }
}
