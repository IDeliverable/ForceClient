using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IDeliverable.ForceClient.Metadata.Archives.Storage;
using IDeliverable.ForceClient.Metadata.Describe;

namespace IDeliverable.ForceClient.Metadata.Archives
{
    public class Package
    {
        internal Package(IArchiveStorage storage, MetadataDescription metadataDescription, string name, string directoryPath)
        {
            mStorage = storage;
            mMetadataDescription = metadataDescription;
            mManifestFilePath = $"{directoryPath}/package.xml";

            Name = name;
            DirectoryPath = directoryPath;
        }

        private readonly IArchiveStorage mStorage;
        private readonly MetadataDescription mMetadataDescription;
        private readonly string mManifestFilePath;

        public string Name { get; }
        public string DirectoryPath { get; }

        public async Task MergeFromAsync(Package other)
        {
            if (other.Name != Name)
                throw new ArgumentException($"Other package has a different name ({other.Name}) than this one ({Name}).", nameof(other));

            var components = await GetComponentsAsync();
            var otherComponents = await other.GetComponentsAsync();

            var componentsToMerge = components.Join(otherComponents, component => component, otherComponent => otherComponent, (component, otherComponent) => (component, otherComponent));
            foreach (var (component, otherComponent) in componentsToMerge)
                await component.MergeFromAsync(otherComponent);

            var componentsToAdd = otherComponents.Except(components);
            foreach (var component in componentsToAdd)
                await ImportComponentAsync(component);
        }

        public async Task<IEnumerable<Component>> GetComponentsAsync()
        {
            var typeDirectoryPatternsQuery =
                from typeDescription in mMetadataDescription.Types.Values
                let typeDirectoryPath = $"{DirectoryPath}/{typeDescription.ArchiveDirectoryName}"
                let pattern = $"*.{typeDescription.ArchiveFileNameExtension}"
                orderby typeDescription.Name
                select (typeDescription, typeDirectoryPath, pattern);

            var result = new List<Component>();

            foreach (var (typeDescription, typeDirectoryPath, pattern) in typeDirectoryPatternsQuery)
            {
                var componentsQuery =
                    from relativeFilePath in await mStorage.ListAsync(typeDirectoryPath, pattern)
                    let fullPath = $"{typeDirectoryPath}/{relativeFilePath}"
                    let name = Path.GetFileNameWithoutExtension(relativeFilePath)
                    orderby name
                    select new Component(mStorage, typeDescription, name, fullPath);
                result.AddRange(componentsQuery);
            }

            return result;
        }

        public async Task<Component> ImportComponentAsync(Component importComponent)
        {
            var typeDescription = mMetadataDescription.Types[importComponent.Type];
            var name = importComponent.Name;
            var fullPath = $"{DirectoryPath}/{typeDescription.ArchiveDirectoryName}/{name}.{typeDescription.ArchiveFileNameExtension}";
            var newComponent = new Component(mStorage, typeDescription, name, fullPath);

            await newComponent.CopyFromAsync(importComponent);

            return newComponent;
        }

        public async Task<PackageManifest> ReadManifestAsync()
        {
            if (!await mStorage.GetExistsAsync(mManifestFilePath))
                return null;

            return await PackageManifest.LoadFromAsync(mStorage, mManifestFilePath);
        }

        public async Task WriteManifestAsync()
        {
            var manifest = await ReadManifestAsync() ?? new PackageManifest();

            manifest.Components.Clear();
            foreach (var component in await GetComponentsAsync())
            {
                manifest.Components.Add((component.Type, component.Name));
                foreach (var nestedComponent in await component.GetNestedComponentsAsync())
                    manifest.Components.Add((nestedComponent.Type, nestedComponent.Name));
            }

            await manifest.SaveToAsync(mStorage, mManifestFilePath);
        }
    }
}
