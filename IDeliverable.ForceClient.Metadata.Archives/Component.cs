using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using IDeliverable.ForceClient.Metadata.Archives.Storage;
using IDeliverable.ForceClient.Metadata.Describe;

namespace IDeliverable.ForceClient.Metadata.Archives
{
    public class Component
    {
        internal Component(IArchiveStorage storage, MetadataTypeDescription typeDescription, string name, string filePath)
        {
            mStorage = storage;
            mTypeDescription = typeDescription;
            Name = name;
            FilePath = filePath;

            // TODO: Create file if it doesn't already exist?
            // TODO: Create meta file if required and it doesn't already exist?
        }

        private readonly IArchiveStorage mStorage;
        private readonly MetadataTypeDescription mTypeDescription;

        public string Type => mTypeDescription.Name;
        public string Name { get; }
        public string FilePath { get; }

        public async Task MergeFromAsync(Component other)
        {
            if (other.mTypeDescription.Name != mTypeDescription.Name)
                throw new ArgumentException($"Other component is of a different type ({other.mTypeDescription.Name}) than this one ({mTypeDescription.Name}).", nameof(other));

            if (!mTypeDescription.HasNestedTypes)
                return;

            // TODO: Maybe check here if it's a valid XML file.

            var doc = await LoadDocumentAsync();
            var nestedComponents = GetNestedComponents(doc);
            var otherNestedComponents = await other.GetNestedComponentsAsync();

            var componentsToAdd = otherNestedComponents.Except(nestedComponents, new NestedComponent.EqualityComparer());
            foreach (var component in componentsToAdd)
                doc.Root.Add(component.Xml);

            await SaveDocumentAsync(doc);
        }

        public async Task<IEnumerable<NestedComponent>> GetNestedComponentsAsync()
        {
            if (!mTypeDescription.HasNestedTypes)
                return null;

            var doc = await LoadDocumentAsync();
            var result = GetNestedComponents(doc);
            return result;
        }

        public Task<Stream> OpenReadAsync()
        {
            return mStorage.OpenReadAsync(FilePath);
        }

        public Task<Stream> OpenWriteAsync()
        {
            return mStorage.OpenWriteAsync(FilePath);
            // TODO: Update the package manifest with any nested components that may have been added/modified/removed.
        }

        public Task WriteAsync(Stream content)
        {
            return mStorage.WriteAsync(FilePath, content);
            // TODO: Update the package manifest with any nested components that may have been added/modified/removed.
        }

        public Task DeleteAsync()
        {
            return mStorage.DeleteAsync(FilePath);
            // TODO: Update the package manifest with any nested components that were.
        }

        private async Task<XDocument> LoadDocumentAsync()
        {
            XDocument doc = null;
            using (var readStream = await OpenReadAsync())
                doc = await XDocument.LoadAsync(readStream, LoadOptions.None, CancellationToken.None);
            return doc;
        }

        private async Task SaveDocumentAsync(XDocument doc)
        {
            using (var writeStream = await OpenWriteAsync())
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

        private IEnumerable<NestedComponent> GetNestedComponents(XDocument doc)
        {
            var nestedElementNameTypes =
                mTypeDescription.NestedTypes
                    .ToDictionary(x => x.ElementName);

            var result =
                doc.Root.Elements()
                    .Where(element => nestedElementNameTypes.ContainsKey(element.Name.LocalName))
                    .Select(element =>
                    {
                        var nestedType = nestedElementNameTypes[element.Name.LocalName];
                        var nestedComponentName = element.Elements().SingleOrDefault(childElement => childElement.Name.LocalName == nestedType.KeyChildElementName)?.Value;
                        return new NestedComponent(nestedType.Name, $"{Name}.{nestedComponentName}", element);
                    })
                    .ToArray();

            return result;
        }
    }
}
