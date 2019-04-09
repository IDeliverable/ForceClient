using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IDeliverable.ForceClient.Metadata.Archives.Storage;
using IDeliverable.ForceClient.Metadata.Describe;

namespace IDeliverable.ForceClient.Metadata.Archives
{
    public class Package : IEquatable<Package>
    {
        internal const string ManifestFileName = "package.xml";

        public static async Task<Package> LoadAsync(IArchiveStorage storage, MetadataDescription metadataDescription, string directoryPath)
        {
            var name = "unpackaged";
            if (!String.IsNullOrEmpty(directoryPath))
                name = directoryPath;
            else
            {
                var manifestFilePath = CombinePath(directoryPath, ManifestFileName);
                if (await storage.GetExistsAsync(manifestFilePath))
                {
                    var manifest = await PackageManifest.LoadAsync(storage, manifestFilePath);
                    if (!String.IsNullOrEmpty(manifest.Name))
                        name = manifest.Name;
                }
            }

            return new Package(storage, metadataDescription, name, directoryPath);
        }

        private static string CombinePath(params string[] parts)
        {
            var nonEmptyPartsQuery =
                from part in parts
                where !String.IsNullOrWhiteSpace(part)
                select part;

            return String.Join("/", nonEmptyPartsQuery);
        }

        internal Package(IArchiveStorage storage, MetadataDescription metadataDescription, string name, string directoryPath)
        {
            mStorage = storage;
            mMetadataDescription = metadataDescription;
            mManifestFilePath = CombinePath(directoryPath, ManifestFileName);

            Name = name;
            DirectoryPath = directoryPath;
        }

        private readonly IArchiveStorage mStorage;
        private readonly MetadataDescription mMetadataDescription;
        private readonly string mManifestFilePath;

        public string Name { get; }
        public string DirectoryPath { get; }

        public bool Equals(Package other)
        {
            if (other == null)
                return false;
            return Name == other.Name;
        }

        public override bool Equals(object other)
        {
            return Equals(other as Package);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public async Task MergeFromAsync(Package other)
        {
            if (other.Name != Name)
                throw new ArgumentException($"Other package has a different name ({other.Name}) than this one ({Name}).", nameof(other));

            if (await other.ReadManifestAsync() is PackageManifest otherManifest)
            {
                var manifest = await ReadManifestAsync() ?? new PackageManifest(Name, otherManifest.ApiVersion);
                manifest.MergePropertiesFrom(otherManifest);
                await manifest.SaveAsync(mStorage, mManifestFilePath);
            }

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
                let typeDirectoryPath = CombinePath(DirectoryPath, typeDescription.ArchiveDirectoryName)
                let pattern = $"*.{typeDescription.ArchiveFileNameExtension}"
                orderby typeDescription.Name
                select (typeDescription, typeDirectoryPath, pattern);

            var result = new List<Component>();

            foreach (var (typeDescription, typeDirectoryPath, pattern) in typeDirectoryPatternsQuery)
            {
                var componentsQuery =
                    from relativeFilePath in await mStorage.ListAsync(typeDirectoryPath, pattern)
                    let fullPath = CombinePath(typeDirectoryPath, relativeFilePath)
                    let name = Path.GetFileNameWithoutExtension(relativeFilePath)
                    orderby name
                    select new Component(mStorage, typeDescription, name, fullPath);
                result.AddRange(componentsQuery);
            }

            return result;
        }

        public async Task<bool> GetComponentExistsAsync(string type, string name)
        {
            var typeDescription = mMetadataDescription.Types[type];
            var fullPath = CombinePath(DirectoryPath, typeDescription.ArchiveDirectoryName, $"{name}.{typeDescription.ArchiveFileNameExtension}");
            return await mStorage.GetExistsAsync(fullPath);
        }

        public async Task<Component> ImportComponentAsync(Component importComponent)
        {
            if (await GetComponentExistsAsync(importComponent.Type, importComponent.Name) == true)
                throw new InvalidOperationException($"Cannot import component of type {importComponent.Type} named '{importComponent.Name}' because it already exists in this package.");

            var typeDescription = mMetadataDescription.Types[importComponent.Type];
            var name = importComponent.Name;
            var fullPath = CombinePath(DirectoryPath, typeDescription.ArchiveDirectoryName, $"{name}.{typeDescription.ArchiveFileNameExtension}");
            var newComponent = new Component(mStorage, typeDescription, name, fullPath);

            await newComponent.CopyFromAsync(importComponent);

            return newComponent;
        }

        public async Task<PackageManifest> ReadManifestAsync()
        {
            if (!await mStorage.GetExistsAsync(mManifestFilePath))
                return null;

            return await PackageManifest.LoadAsync(mStorage, mManifestFilePath);
        }

        //public async Task ImportManifestAsync(PackageManifest importManifest)
        //{
        //    if (await mStorage.GetExistsAsync(mManifestFilePath))
        //        throw new InvalidOperationException("Cannot import package manifest because one already exists in this package.");
        //    if (importManifest.Name != Name)
        //        throw new InvalidOperationException($"Cannot import package manifest because it is for a different package name ({importManifest.Name}) than this package ({Name}).");

        //    await importManifest.SaveAsync(mStorage, mManifestFilePath);
        //}

        public async Task<PackageManifest> WriteManifestAsync(double defaultApiVersion)
        {
            var manifest = await ReadManifestAsync() ?? new PackageManifest(Name, defaultApiVersion);

            manifest.Components.Clear();
            foreach (var component in await GetComponentsAsync())
            {
                manifest.Components.Add((component.Type, component.Name));
                foreach (var nestedComponent in await component.GetNestedComponentsAsync())
                    manifest.Components.Add((nestedComponent.Type, nestedComponent.Name));
            }

            await manifest.SaveAsync(mStorage, mManifestFilePath);

            return manifest;
        }

        public Task DeleteAsync()
        {
            throw new NotImplementedException();
        }
    }
}
