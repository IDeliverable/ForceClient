using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace IDeliverable.ForceClient.Metadata.Archives.Storage
{
	/// <summary>
	/// Represents a generic abstraction of an underlying physical file storage mechanism.
	/// </summary>
	/// <remarks>
	/// Paths are always represented as relative to the storage root, without any leading
	/// drive letters, and without any leading or trailing slashes.
	/// 
	/// Forward slash is always used as the directory separator. If the underlying storage
	/// system uses different directory separation characters, the implementation is responsible
	/// for mapping between the two.
	/// 
	/// The storage abstraction has no notion of directories as stand-alone entries. Directories  
	/// are only used as logical namespaces in file paths. Each implementation is free to either 
	/// represent directories as some physical manifestation in the underlying storage system, or 
	/// treat them merely as part of a file's name.
	/// </remarks>
	public interface IArchiveStorage
	{
		/// <summary>
		/// Lists all files matching a wildcard pattern within a logical directory and all subdirectories.
		/// </summary>
		/// <param name="directoryPath">The locigal directory path in which to list files. <c>null</c> or empty string to </param>
		/// <param name="pattern">A wildcard pattern to include only files matching this pattern, or <c>null</c> to include all files.</param>
		/// <returns>The list of paths of any matching files found, relative to the specified logical directory.</returns>
		Task<IEnumerable<string>> ListAsync(string directoryPath, string pattern = null);

		/// <summary>
		/// Opens a file for reading.
		/// </summary>
		/// <param name="filePath">The path of the file to open for reading.</param>
		/// <returns>A <see cref="Stream"/> that can be used to read the contents of the file. The caller is responsible for closing the <see cref="Stream"/>.</returns>
		/// <exception cref="StorageFileNotFoundException">The specified file was not found.</exception>
		Task<Stream> OpenReadAsync(string filePath);

		/// <summary>
		/// Opens a file for writing.
		/// </summary>
		/// <param name="filePath">The path of the file to open for writing. The file will be created if it doesn't exist.</param>
		/// <returns>A <see cref="Stream"/> that can be used to write to the file. The caller is responsible for closing the stream.</returns>
		Task<Stream> OpenWriteAsync(string filePath);

		/// <summary>
		/// Writes the content of the provided <see cref="Stream"/> to a file.
		/// </summary>
		/// <param name="filePath">The path of the file to write to. The file will be created if it doesn't exist.</param>
		/// <param name="content">A <see cref="Stream"/> containing the content to write to the file. The <see cref="Stream"/> will be read from its current position and will not be closed by this method.</param>
		Task WriteAsync(string filePath, Stream content);

		/// <summary>
		/// Deletes a file.
		/// </summary>
		/// <param name="filePath">The path of the file to delete. If the file doesn't exist, this method will simply return.</param>
		Task DeleteAsync(string filePath);

		/// <summary>
		/// Returns metadata properties of a file.
		/// </summary>
		/// <param name="filePath">The path of the file whose properties to return.</param>
		/// <returns>A <see cref="FileProperties"/> object containing the metadata properties of the file, or <c>null</c> if the file does not exist.</returns>
		Task<FileProperties> GetPropertiesAsync(string filePath);

		/// <summary>
		/// Returns a <see cref="Boolean"/> indicating whether a file exists or not.
		/// </summary>
		/// <param name="filePath">The path of the file to check.</param>
		/// <returns><c>true</c> if the file exists, otherwise <c>false</c>.</returns>
		Task<bool> GetExistsAsync(string filePath);
	}

	public static class IArchiveStorageExtentions
	{
		public static async Task LoadFromZipAsync(this IArchiveStorage storage, byte[] zipBytes)
		{
			using (var zipStream = new MemoryStream(zipBytes))
				await storage.LoadFromZipAsync(zipStream);
		}

		public static async Task LoadFromZipAsync(this IArchiveStorage storage, Stream zipStream)
		{
			using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read))
				// TODO: Speed this up using a simple buffered TPL pipeline.
				foreach (var zipEntry in zipArchive.Entries)
					using (var zipEntryStream = zipEntry.Open())
						await storage.WriteAsync(zipEntry.FullName, zipEntryStream);
		}

		public static async Task SaveToZipAsync(this IArchiveStorage storage, byte[] zipBytes)
		{
			using (var zipStream = new MemoryStream(zipBytes))
				await storage.SaveToZipAsync(zipStream);
		}

		public static async Task SaveToZipAsync(this IArchiveStorage storage, Stream zipStream)
		{
			using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create))
			{
				var filePathsQuery =
					from filePath in await storage.ListAsync(directoryPath: null)
					where !filePath.StartsWith(".")
					select filePath;

				// TODO: Speed this up using a simple buffered TPL pipeline.
				foreach (var filePath in filePathsQuery)
				{
					var zipEntry = zipArchive.CreateEntry(filePath);
					using (Stream fileStream = await storage.OpenReadAsync(filePath), zipEntryStream = zipEntry.Open())
						await fileStream.CopyToAsync(zipEntryStream);
				}
			}
		}
	}
}
