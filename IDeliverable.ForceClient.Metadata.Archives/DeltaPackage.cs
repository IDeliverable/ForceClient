using System.Collections.Generic;
using System.Threading.Tasks;
using IDeliverable.ForceClient.Metadata.Archives.Storage;
using IDeliverable.ForceClient.Metadata.Describe;

namespace IDeliverable.ForceClient.Metadata.Archives
{
	public class DeltaPackage : Package
	{
		internal const string DeletePreManifestFileName = "destructiveChangesPre.xml";
		internal const string DeletePostManifestFileName = "destructiveChangesPost.xml";

		internal DeltaPackage(IArchiveStorage storage, MetadataDescription metadataDescription, string name, string directoryPath)
			: base(storage, metadataDescription, name, directoryPath)
		{
			mStorage = storage;
			mDeletePreManifestFilePath = CombinePath(directoryPath, DeletePreManifestFileName);
			mDeletePostManifestFilePath = CombinePath(directoryPath, DeletePostManifestFileName);

			DeletePreComponents = new HashSet<(string, string)>();
			DeletePostComponents = new HashSet<(string, string)>();

			ReadDeleteManifestsAsync().Wait(); // TODO: Think of a better way; should not block on asynchronous operation in a constructor.
		}

		private readonly IArchiveStorage mStorage;
		private readonly string mDeletePreManifestFilePath;
		private readonly string mDeletePostManifestFilePath;

		public ISet<(string type, string name)> DeletePreComponents { get; }
		public ISet<(string type, string name)> DeletePostComponents { get; }

		public async Task ReadDeleteManifestsAsync()
		{
			DeletePreComponents.Clear();
			DeletePostComponents.Clear();

			if (await mStorage.GetExistsAsync(mDeletePreManifestFilePath))
			{
				var deletePreManifest = await PackageManifest.LoadAsync(mStorage, mDeletePreManifestFilePath);
				foreach (var component in deletePreManifest.Components)
					DeletePreComponents.Add(component);
			}

			if (await mStorage.GetExistsAsync(mDeletePostManifestFilePath))
			{
				var deletePostManifest = await PackageManifest.LoadAsync(mStorage, mDeletePostManifestFilePath);
				foreach (var component in deletePostManifest.Components)
					DeletePostComponents.Add(component);
			}
		}

		public async Task WriteDeleteManifestsAsync()
		{
			if (DeletePreComponents.Count > 0)
			{
				var deletePreManifest = new PackageManifest(DeletePreComponents);
				await deletePreManifest.SaveAsync(mStorage, mDeletePreManifestFilePath);
			}
			else
				await mStorage.DeleteAsync(mDeletePreManifestFilePath);

			if (DeletePostComponents.Count > 0)
			{
				var deletePostManifest = new PackageManifest(DeletePostComponents);
				await deletePostManifest.SaveAsync(mStorage, mDeletePostManifestFilePath);
			}
			else
				await mStorage.DeleteAsync(mDeletePostManifestFilePath);
		}
	}
}
