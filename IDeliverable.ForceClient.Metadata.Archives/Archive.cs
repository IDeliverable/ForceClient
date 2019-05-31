using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IDeliverable.ForceClient.Metadata.Archives.Storage;
using IDeliverable.ForceClient.Metadata.Describe;

namespace IDeliverable.ForceClient.Metadata.Archives
{
	public class Archive
	{
		public static async Task<Archive> CreateAsync(IArchiveStorage storage, MetadataDescription metadataDescription, bool isSinglePackage)
		{
			var filePaths = await storage.ListAsync(directoryPath: null);

			var existingFilesQuery =
				from filePath in filePaths
				where !filePath.StartsWith(".")
				select filePath;

			if (existingFilesQuery.Any())
				throw new Exception("Could not create archive because the specified specified storage is not empty.");

			return new Archive(storage, metadataDescription, isSinglePackage);
		}

		public static async Task<Archive> LoadAsync(IArchiveStorage storage, MetadataDescription metadataDescription)
		{
			// INFO: We use the following logic for determining whether an archive is a single-package archive:
			// 1. package.xml in root => single-package
			// 2. No subdirectories => assume single-package
			// 3. One or more package.xml in subdirectories => multi-package
			// 4. No package.xml at all =>
			//    Look at all subdirectory names which do not start with a dot (e.g. .git or .vs):
			//    5. More than 90% of subdirectory names match known metadata types => single-package
			//    6. No subdirectory names match known metadata types => multi-package
			//    7. Otherwise => inconsistent archive format, throw error

			bool isSinglePackage;
			if (await storage.GetExistsAsync(Package.ManifestFileName))
				isSinglePackage = true;
			else
			{
				var filePaths = await storage.ListAsync(directoryPath: null);
				var subdirectoryNames = GetSubdirectoryNames(filePaths);

				if (!subdirectoryNames.Any())
					isSinglePackage = true;
				else
				{
					var packageManifestFilePathsQuery =
						from filePath in filePaths
						where filePath.EndsWith(Package.ManifestFileName)
						select filePath;

					if (packageManifestFilePathsQuery.Any())
						isSinglePackage = false;
					else
					{
						var knownMetadataDirectoryNames =
								metadataDescription.Types.Values
									.Select(type => type.ArchiveDirectoryName)
									.Distinct()
									.ToArray();

						if (subdirectoryNames.All(subdirectoryName => knownMetadataDirectoryNames.Contains(subdirectoryName)))
							isSinglePackage = true;
						else if (!subdirectoryNames.Any(subdirectoryName => knownMetadataDirectoryNames.Contains(subdirectoryName)))
							isSinglePackage = false;
						else
						{
							var nonMetadataDirectoryNamesQuery =
								from subdirectoryName in subdirectoryNames
								where !knownMetadataDirectoryNames.Contains(subdirectoryName)
								select subdirectoryName;

							if (nonMetadataDirectoryNamesQuery.Count() > subdirectoryNames.Count() * 0.1)
								throw new Exception($"Could not determine whether archive is single-package or multi-package; archive contains no package manifests and contains both known metadata subdirectories and the following non-metadata subdirectories: {String.Join(", ", nonMetadataDirectoryNamesQuery)}");

							isSinglePackage = true;
						}
					}
				}
			}

			return new Archive(storage, metadataDescription, isSinglePackage);
		}

		private static IEnumerable<string> GetSubdirectoryNames(IEnumerable<string> filePaths)
		{
			var subdirectoryNamesQuery =
				from filePath in filePaths
				where !filePath.StartsWith(".")
				let firstSlashIndex = filePath.IndexOf("/")
				where firstSlashIndex > -1
				select filePath.Substring(0, firstSlashIndex);
			return subdirectoryNamesQuery.Distinct().ToArray();
		}

		private Archive(IArchiveStorage storage, MetadataDescription metadataDescription, bool isSinglePackage)
		{
			mStorage = storage;
			MetadataDescription = metadataDescription;
			IsSinglePackage = isSinglePackage;
		}

		private readonly IArchiveStorage mStorage;

		public MetadataDescription MetadataDescription { get; }
		public bool IsSinglePackage { get; }

		public async Task CleanAsync()
		{
			var packages = await GetPackagesAsync();
			foreach (var package in packages)
				await package.CleanAsync();
		}

		public async Task MergeFromAsync(Archive other)
		{
			// INFO: Merging another archive into this archive is done primarily to support
			// retrieving metadata archives whose size exceed the 10000 component limit imposed
			// by the metadata API. As such, merging is supported when:
			// - Both archives are single-package and their respective package names match
			// - Both archives are multi-package
			// - Source package is single-package and target is multi-package
			// Merging a multi-package into a single-package archive is not supported.
			// Merging a single-package into a single-package archive where package names are different is not supported.
			if (IsSinglePackage)
			{
				if (!other.IsSinglePackage)
					throw new InvalidOperationException("Cannot merge a multi-package archive into a single-package archive.");
				var package = await GetSinglePackageAsync();
				var otherPackage = await other.GetSinglePackageAsync();
				if (otherPackage.Name != package.Name)
					throw new InvalidOperationException("Cannot merge two single-package archives with different package names.");
			}

			var packages = await GetPackagesAsync();
			var otherPackages = await other.GetPackagesAsync();

			var packagesToMerge = packages.Join(otherPackages, package => package, otherPackage => otherPackage, (package, otherPackage) => (package, otherPackage));
			foreach (var (package, otherPackage) in packagesToMerge)
				await package.MergeFromAsync(otherPackage, propertiesOnly: false);

			var packagesToAdd = otherPackages.Except(packages);
			foreach (var package in packagesToAdd)
				_ = await ImportPackageAsync(package);
		}

		public async Task WriteDeltaSinceAsync(Archive previous, Archive outputDelta)
		{
			var packages = await GetPackagesAsync();
			var previousPackages = await previous.GetPackagesAsync();

			var packagePairs = packages.Join(previousPackages, package => package.Name, package => package.Name, (package, previousPackage) => (package, previousPackage));
			foreach (var (package, previousPackage) in packagePairs)
			{
				var deltaPackage = await outputDelta.AddNewDeltaPackageAsync(package.Name);
				await deltaPackage.MergeFromAsync(package, propertiesOnly: true);
				await package.CreateDeltaSinceAsync(previousPackage, deltaPackage);
				await deltaPackage.WriteManifestAsync(defaultApiVersion: null);
				await deltaPackage.WriteDeleteManifestsAsync();
			}
		}

		public async Task<Package> GetSinglePackageAsync()
		{
			if (!IsSinglePackage)
				throw new InvalidOperationException("Cannot get single package because this is not a single-package archive.");

			return (await GetPackagesAsync()).Single();
		}

		public async Task<IEnumerable<Package>> GetPackagesAsync()
		{
			if (IsSinglePackage)
				return new[] { await Package.LoadAsync(mStorage, MetadataDescription, directoryPath: null) };

			var filePaths = await mStorage.ListAsync(directoryPath: null);
			var subdirectoryNames = GetSubdirectoryNames(filePaths);

			var packages = new List<Package>();
			foreach (var subdirectoryName in subdirectoryNames)
				packages.Add(await Package.LoadAsync(mStorage, MetadataDescription, subdirectoryName));

			return packages.ToArray();
		}

		public async Task<bool> GetPackageExistsAsync(string name)
		{
			// TODO: There may be a more efficient way to do this that doesn't entail instantiating all packages.
			var packages = await GetPackagesAsync();
			return packages.Any(package => package.Name == name);
		}

		public async Task<DeltaPackage> AddNewDeltaPackageAsync(string name)
		{
			if (IsSinglePackage)
			{
				var packages = await GetPackagesAsync();
				if (packages.Any())
					throw new InvalidOperationException("Cannot add additional packages in a single-package archive.");
			}

			var newPackageDirectoryPath = IsSinglePackage ? null : name;
			var newPackage = new DeltaPackage(mStorage, MetadataDescription, name, newPackageDirectoryPath);
			await newPackage.WriteManifestAsync(defaultApiVersion: null);

			return newPackage;
		}

		public async Task<Package> ImportPackageAsync(Package importPackage)
		{
			if (IsSinglePackage)
			{
				var packages = await GetPackagesAsync();
				if (packages.Any())
					throw new InvalidOperationException("Cannot import additional packages into a single-package archive.");
			}

			if (await GetPackageExistsAsync(importPackage.Name) == true)
				throw new InvalidOperationException($"Cannot import package '{importPackage.Name}' because it already exists in this archive.");

			var newPackageDirectoryPath = IsSinglePackage ? null : importPackage.Name;
			var newPackage = new Package(mStorage, MetadataDescription, importPackage.Name, newPackageDirectoryPath);
			await newPackage.MergeFromAsync(importPackage, propertiesOnly: false);

			return newPackage;
		}
	}
}
