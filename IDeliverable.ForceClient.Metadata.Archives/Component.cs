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
    public class Component : IEquatable<Component>
    {
        internal Component(IArchiveStorage storage, MetadataTypeDescription typeDescription, string name, string filePath)
        {
            mStorage = storage;
            mTypeDescription = typeDescription;

            Name = name;
            FilePath = filePath;

            if (mTypeDescription.HasMetaFile)
                MetaFilePath = $"{FilePath}-meta.xml";

            // TODO: Create file if it doesn't already exist?
            // TODO: Create meta file if required and it doesn't already exist?
        }

        private readonly IArchiveStorage mStorage;
        private readonly MetadataTypeDescription mTypeDescription;

        public string Type => mTypeDescription.Name;
        public string Name { get; }
        public string FilePath { get; }
        public string MetaFilePath { get; }

        public bool Equals(Component other)
        {
            return Type == other.Type && Name == other.Name;
        }

        public async Task MergeFromAsync(Component other)
        {
            if (other.Type != Type)
                throw new ArgumentException($"Other component is of a different type ({other.Type}) than this one ({Type}).", nameof(other));
            if (other.Name != Name)
                throw new ArgumentException($"Other component has a different name ({other.Name}) than this one ({Name}).", nameof(other));

            if (!mTypeDescription.HasNestedTypes)
                return;

            // TODO: Maybe check here if it's a valid XML file. For now, the assumption is that if the type has nested types, it must be in XML format.

            var doc = await LoadDocumentAsync();
            var nestedComponents = GetNestedComponents(doc);
            var otherNestedComponents = await other.GetNestedComponentsAsync();

            var componentsToAdd = otherNestedComponents.Except(nestedComponents);
            foreach (var component in componentsToAdd)
                doc.Root.Add(component.Xml);

            await SaveDocumentAsync(doc);
        }

        public async Task CopyFromAsync(Component other)
        {
            using (var readStream = await other.OpenReadAsync())
                await other.WriteAsync(readStream);

            if (mTypeDescription.HasMetaFile)
            {
                using (var metaFileReadStream = await other.OpenMetaFileReadAsync())
                    await other.WriteMetaFileAsync(metaFileReadStream);
            }
        }

        public async Task<IEnumerable<NestedComponent>> GetNestedComponentsAsync()
        {
            if (!mTypeDescription.HasNestedTypes)
                return Enumerable.Empty<NestedComponent>();

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
        }

        public Task WriteAsync(Stream content)
        {
            return mStorage.WriteAsync(FilePath, content);
        }

        public Task<Stream> OpenMetaFileReadAsync()
        {
            if (!mTypeDescription.HasMetaFile)
                throw new Exception($"Components of type {Type} do not have meta files.");

            return mStorage.OpenReadAsync(MetaFilePath);
        }

        public Task<Stream> OpenMetaFileWriteAsync()
        {
            if (!mTypeDescription.HasMetaFile)
                throw new Exception($"Components of type {Type} do not have meta files.");

            return mStorage.OpenWriteAsync(MetaFilePath);
        }

        public Task WriteMetaFileAsync(Stream content)
        {
            if (!mTypeDescription.HasMetaFile)
                throw new Exception($"Components of type {Type} do not have meta files.");

            return mStorage.WriteAsync(MetaFilePath, content);
        }

        public async Task DeleteAsync()
        {
            await mStorage.DeleteAsync(FilePath);

            if (mTypeDescription.HasMetaFile)
                await mStorage.DeleteAsync(MetaFilePath);
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
