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

		protected static string CombinePath(params string[] parts)
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

		public async Task MergeFromAsync(Package other, bool propertiesOnly)
		{
			if (other.Name != Name)
				throw new ArgumentException($"Other package has a different name ({other.Name}) than this one ({Name}).", nameof(other));

			if (await other.ReadManifestAsync() is PackageManifest otherManifest)
			{
				var manifest = await ReadManifestAsync() ?? new PackageManifest(Name, otherManifest.ApiVersion);
				manifest.MergePropertiesFrom(otherManifest);
				await manifest.SaveAsync(mStorage, mManifestFilePath);
			}

			if (!propertiesOnly)
			{
				var components = await GetComponentsAsync();
				var otherComponents = await other.GetComponentsAsync();

				var componentsToMerge = components.Join(otherComponents, component => component, otherComponent => otherComponent, (component, otherComponent) => (component, otherComponent));
				foreach (var (component, otherComponent) in componentsToMerge)
					await component.MergeFromAsync(otherComponent);

				var componentsToAdd = otherComponents.Except(components);
				foreach (var component in componentsToAdd)
					await ImportComponentAsync(component);
			}
		}

		public async Task CreateDeltaSinceAsync(Package previous, DeltaPackage delta)
		{
			if (delta.Name != Name)
				throw new ArgumentException($"Delta package has a different name ({delta.Name}) than this one ({Name}).", nameof(previous));

			var diff = await GetDiffSinceAsync(previous);

			foreach (var addedComponent in diff.Added)
				await delta.ImportComponentAsync(addedComponent);
			foreach (var modifiedComponent in diff.Modified)
				await delta.ImportComponentAsync(modifiedComponent);
			foreach (var deletedComponent in diff.Deleted)
				delta.DeletePostComponents.Add(deletedComponent);
		}

		public async Task<PackageDiff> GetDiffSinceAsync(Package previous)
		{
			if (previous.Name != Name)
				throw new ArgumentException($"Previous package has a different name ({previous.Name}) than this one ({Name}).", nameof(previous));

			var components = await GetComponentsAsync();
			var previousComponents = await previous.GetComponentsAsync();
			var componentPairs = components.Join(previousComponents, component => (component.Type, component.Name), component => (component.Type, component.Name), (component, previousComponent) => (component, previousComponent));

			var addedComponents = components.Except(previousComponents);
			var modifiedComponents = new HashSet<Component>();
			var deletedComponents =
				previousComponents
					.Except(components)
					.Select(component => (type: component.Type, name: component.Name))
					.ToHashSet();

			foreach (var (component, previousComponent) in componentPairs)
			{
				if (await component.GetIsModifiedSinceAsync(previousComponent))
				{
					modifiedComponents.Add(component);

					// Only if the component is modified do we need to check for deleted nested components.
					foreach (var deletedNestedComponent in await component.GetDeletedNestedComponentsSinceAsync(previousComponent))
						deletedComponents.Add(deletedNestedComponent);
				}
			}

			return new PackageDiff(addedComponents.ToArray(), modifiedComponents, deletedComponents);
		}

		public async Task<IEnumerable<Component>> GetComponentsAsync()
		{
			var typeDirectoryPatternsQuery =
				from typeDescription in mMetadataDescription.Types.Values
				let typeDirectoryPath = CombinePath(DirectoryPath, typeDescription.ArchiveDirectoryName)
				let pattern = !String.IsNullOrEmpty(typeDescription.ArchiveFileNameExtension) ? $"*.{typeDescription.ArchiveFileNameExtension}" : null
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

			return result.ToArray();
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

		public async Task WriteManifestAsync(double? defaultApiVersion)
		{
			var components = await GetComponentsAsync();

			var manifest = await ReadManifestAsync() ?? new PackageManifest(Name, defaultApiVersion);

			manifest.Components.Clear();
			foreach (var component in components)
			{
				manifest.Components.Add((component.Type, component.Name));
				foreach (var nestedComponent in await component.GetNestedComponentsAsync())
					manifest.Components.Add((nestedComponent.Type, nestedComponent.Name));
			}

			await manifest.SaveAsync(mStorage, mManifestFilePath);
		}

		public Task DeleteAsync()
		{
			// TODO: Implement Package.DeleteAsync()
			throw new NotImplementedException();
		}
	}
}
