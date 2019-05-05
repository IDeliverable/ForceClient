using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IDeliverable.ForceClient.Metadata.Archives.Storage
{
	/// <summary>
	/// Provides an <see cref="IArchiveStorage"/> implementation that uses a <see cref="System.IO"/>-based
	/// file system directory as its underlying storage mechanism.
	/// </summary>
	public class DirectoryArchiveStorage : IArchiveStorage
	{
		/// <summary>
		/// Creates a new <see cref="DirectoryArchiveStorage"/> instance.
		/// </summary>
		/// <param name="basePath">The absolute file system path of the directory to use as the root storage container. The <see cref="DirectoryArchiveStorage"/> instance will store all files underneath this directory.</param>
		/// <param name="logger">An <see cref="ILogger"/> instance to use for logging.</param>
		public DirectoryArchiveStorage(string basePath, ILogger<DirectoryArchiveStorage> logger)
		{
			var expandedBasePath = Environment.ExpandEnvironmentVariables(basePath);

			mLogger = logger;

			mLogger.LogDebug($"Configured base path '{basePath}' expanded to '{expandedBasePath}'.");

			Directory.CreateDirectory(expandedBasePath);

			if (!Directory.Exists(expandedBasePath))
				throw new ArgumentException($"The specified base path '{basePath}' (expanded to '{expandedBasePath}') does not exist or is not a directory.", nameof(basePath));

			BasePath = expandedBasePath;
		}

		private readonly ILogger mLogger;

		public string BasePath { get; }

		public virtual Task<IEnumerable<string>> ListAsync(string directoryPath, string pattern = null)
		{
			var normalizedDirectoryPath = NormalizePath(directoryPath);
			var absoluteDirectoryPath = Path.Combine(BasePath, normalizedDirectoryPath) + Path.DirectorySeparatorChar;

			if (!Directory.Exists(absoluteDirectoryPath))
				return Task.FromResult<IEnumerable<string>>(new string[] { });

			try
			{
				var matchingFilePathQuery =
					from filePath in Directory.EnumerateFiles(absoluteDirectoryPath, pattern ?? "*", SearchOption.AllDirectories)
					where filePath.StartsWith(absoluteDirectoryPath, StringComparison.InvariantCultureIgnoreCase)
					select filePath.Substring(absoluteDirectoryPath.Length).Replace(Path.DirectorySeparatorChar, '/');

				return Task.FromResult<IEnumerable<string>>(matchingFilePathQuery.ToArray());
			}
			catch (Exception ex)
			{
				throw new StorageException($"Error while listing files in directory '{normalizedDirectoryPath}'.", ex);
			}
		}

		public virtual Task<Stream> OpenReadAsync(string filePath)
		{
			var normalizedFilePath = NormalizePath(filePath);

			try
			{
				var absoluteFilePath = Path.Combine(BasePath, normalizedFilePath);

				return Task.FromResult<Stream>(File.OpenRead(absoluteFilePath));
			}
			catch (FileNotFoundException)
			{
				throw new StorageFileNotFoundException(normalizedFilePath);
			}
			catch (Exception ex)
			{
				throw new StorageException($"Error while opening file '{normalizedFilePath}' for reading.", ex);
			}
		}

		public virtual Task<Stream> OpenWriteAsync(string filePath)
		{
			var normalizedFilePath = NormalizePath(filePath);

			try
			{
				var absoluteFilePath = Path.Combine(BasePath, normalizedFilePath);

				// Ensure target directory exists.
				Directory.CreateDirectory(Path.GetDirectoryName(absoluteFilePath));

				return Task.FromResult<Stream>(File.Create(absoluteFilePath));
			}
			catch (Exception ex)
			{
				throw new StorageException($"Error while opening file '{normalizedFilePath}' for writing.", ex);
			}
		}

		public virtual async Task WriteAsync(string filePath, Stream content)
		{
			var normalizedFilePath = NormalizePath(filePath);

			try
			{
				var absoluteFilePath = Path.Combine(BasePath, normalizedFilePath);

				// Ensure target directory exists.
				Directory.CreateDirectory(Path.GetDirectoryName(absoluteFilePath));

				using (var writeStream = File.Create(absoluteFilePath))
					await content.CopyToAsync(writeStream);
			}
			catch (Exception ex)
			{
				throw new StorageException($"Error while writing to file '{normalizedFilePath}'.", ex);
			}
		}

		public virtual Task DeleteAsync(string filePath)
		{
			var normalizedFilePath = NormalizePath(filePath);

			try
			{
				var absoluteFilePath = Path.Combine(BasePath, normalizedFilePath);

				if (File.Exists(absoluteFilePath))
					File.Delete(absoluteFilePath);

				return Task.CompletedTask;
			}
			catch (Exception ex)
			{
				throw new StorageException($"Error while deleting file '{normalizedFilePath}'.", ex);
			}
		}

		public virtual Task<FileProperties> GetPropertiesAsync(string filePath)
		{
			var normalizedFilePath = NormalizePath(filePath);

			try
			{
				var absoluteFilePath = Path.Combine(BasePath, normalizedFilePath);

				if (!File.Exists(absoluteFilePath))
					return null;

				var fileInfo = new FileInfo(absoluteFilePath);
				var result = new FileProperties(normalizedFilePath, fileInfo.Length, fileInfo.LastWriteTimeUtc);
				return Task.FromResult(result);
			}
			catch (Exception ex)
			{
				throw new StorageException($"Error while getting size of file '{normalizedFilePath}'.", ex);
			}
		}

		public virtual Task<bool> GetExistsAsync(string filePath)
		{
			var normalizedFilePath = NormalizePath(filePath);

			try
			{
				var absoluteFilePath = Path.Combine(BasePath, normalizedFilePath);

				return Task.FromResult(File.Exists(absoluteFilePath));
			}
			catch (Exception ex)
			{
				throw new StorageException($"Error while checking existence of file '{normalizedFilePath}'.", ex);
			}
		}

		protected virtual string NormalizePath(string path)
		{
			if (String.IsNullOrWhiteSpace(path))
				return "";

			return path
				.Trim('\\', '/')
				.Replace('\\', Path.DirectorySeparatorChar)
				.Replace('/', Path.DirectorySeparatorChar);
		}
	}
}
