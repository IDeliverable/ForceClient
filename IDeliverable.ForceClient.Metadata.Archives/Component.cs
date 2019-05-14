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
		public static IEnumerable<NestedComponent> ParseNestedComponents(MetadataTypeDescription componentTypeDescription, string componentName, XDocument doc)
		{
			var nestedElementNameTypes =
				componentTypeDescription.NestedTypes
					.ToDictionary(x => x.ElementName);

			var result =
				doc.Root.Elements()
					.Where(element => nestedElementNameTypes.ContainsKey(element.Name.LocalName))
					.Select(element =>
					{
						var nestedTypeDescription = nestedElementNameTypes[element.Name.LocalName];
						var nestedComponentName = element.Elements().SingleOrDefault(childElement => childElement.Name.LocalName == nestedTypeDescription.KeyChildElementName)?.Value;
						return new NestedComponent(nestedTypeDescription, $"{componentName}.{nestedComponentName}", element);
					})
					.ToArray();

			return result;
		}

		internal Component(IArchiveStorage storage, MetadataTypeDescription typeDescription, string name, string filePath)
		{
			mStorage = storage;

			TypeDescription = typeDescription;
			Name = name;
			FilePath = filePath;

			if (TypeDescription.HasMetaFile)
				MetaFilePath = $"{FilePath}{Package.MetaFileNameSuffix}";
		}

		private readonly IArchiveStorage mStorage;

		public string Type => TypeDescription.Name;
		public MetadataTypeDescription TypeDescription { get; }
		public string Name { get; }
		public string FilePath { get; }
		public string MetaFilePath { get; }

		public bool Equals(Component other)
		{
			if (other == null)
				return false;
			return Type == other.Type && Name == other.Name;
		}

		public override bool Equals(object other)
		{
			return Equals(other as Component);
		}

		public override int GetHashCode()
		{
			return (Type, Name).GetHashCode();
		}

		public async Task MergeFromAsync(Component other)
		{
			VerifyIsSame(other);

			if (!TypeDescription.HasNestedTypes)
				return;

			// Maybe check here if it's a valid XML file. For now, the assumption is that if the type has nested types, it must be in XML format.

			var doc = await LoadDocumentAsync();
			var nestedComponents = ParseNestedComponents(TypeDescription, Name, doc);
			var otherNestedComponents = await other.GetNestedComponentsAsync();

			var componentsToAdd = otherNestedComponents.Except(nestedComponents);
			foreach (var component in componentsToAdd)
				doc.Root.Add(component.Xml);

			await SaveDocumentAsync(doc);
		}

		public async Task CopyFromAsync(Component other)
		{
			VerifyIsSame(other);

			using (var readStream = await other.OpenReadAsync())
				await WriteAsync(readStream);

			if (TypeDescription.HasMetaFile)
			{
				using (var metaFileReadStream = await other.OpenMetaFileReadAsync())
					await WriteMetaFileAsync(metaFileReadStream);
			}
		}

		public async Task<bool> GetIsModifiedSinceAsync(Component previous)
		{
			VerifyIsSame(previous);

			using (Stream thisStream = await OpenReadAsync(), previousStream = await previous.OpenReadAsync())
			{
				if (thisStream.Length != previousStream.Length)
					return true;

				for (var i = 0; i < thisStream.Length; i++)
				{
					if (thisStream.ReadByte() != previousStream.ReadByte())
						return true;
				}
			}

			return false;
		}

		public async Task<IEnumerable<(string type, string name)>> GetDeletedNestedComponentsSinceAsync(Component previous)
		{
			VerifyIsSame(previous);

			if (!TypeDescription.HasNestedTypes)
				return Enumerable.Empty<(string type, string name)>();

			var nested = await GetNestedComponentsAsync();
			var previousNested = await previous.GetNestedComponentsAsync();

			var result =
				previousNested
					.Except(nested)
					.Select(component => (component.Type, component.Name))
					.ToArray();

			return result;
		}

		public async Task<IEnumerable<NestedComponent>> GetNestedComponentsAsync()
		{
			if (!TypeDescription.HasNestedTypes)
				return Enumerable.Empty<NestedComponent>();

			var doc = await LoadDocumentAsync();
			var result = ParseNestedComponents(TypeDescription, Name, doc);
			return result;
		}

		public async Task<XDocument> LoadDocumentAsync()
		{
			XDocument doc = null;
			using (var readStream = await OpenReadAsync())
				doc = await XDocument.LoadAsync(readStream, LoadOptions.None, CancellationToken.None);
			return doc;
		}

		public async Task SaveDocumentAsync(XDocument doc)
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
			if (!TypeDescription.HasMetaFile)
				throw new Exception($"Components of type {Type} do not have meta files.");

			return mStorage.OpenReadAsync(MetaFilePath);
		}

		public Task<Stream> OpenMetaFileWriteAsync()
		{
			if (!TypeDescription.HasMetaFile)
				throw new Exception($"Components of type {Type} do not have meta files.");

			return mStorage.OpenWriteAsync(MetaFilePath);
		}

		public Task WriteMetaFileAsync(Stream content)
		{
			if (!TypeDescription.HasMetaFile)
				throw new Exception($"Components of type {Type} do not have meta files.");

			return mStorage.WriteAsync(MetaFilePath, content);
		}

		public async Task DeleteAsync()
		{
			await mStorage.DeleteAsync(FilePath);

			if (TypeDescription.HasMetaFile)
				await mStorage.DeleteAsync(MetaFilePath);
		}

		private void VerifyIsSame(Component other)
		{
			if (other.Type != Type)
				throw new ArgumentException($"Other component is of a different type ({other.Type}) than this one ({Type}).", nameof(other));
			if (other.Name != Name)
				throw new ArgumentException($"Other component has a different name ({other.Name}) than this one ({Name}).", nameof(other));
		}
	}
}
