using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IDeliverable.ForceClient.Core.OrgAccess;
using IDeliverable.ForceClient.Core.OrgAccess.Native;
using IDeliverable.ForceClient.Metadata.Client;
using IDeliverable.ForceClient.Metadata.Processes;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;
using Serilog.Events;

namespace IDeliverable.ForceClient.Tools.Metadata
{
	class Program
	{
		private static async Task Main(string[] args)
		{
			Log.Logger =
				new LoggerConfiguration()
					.MinimumLevel.Information()
					.MinimumLevel.Override(typeof(NativeClientOrgAccessProvider).FullName, LogEventLevel.Debug)
					.WriteTo.Console()
					.CreateLogger();

			var services =
				new ServiceCollection()
					.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true))
					.AddForceClient(configure =>
					{
						var clientId = "3MVG9A_f29uWoVQvrJSnfk5LPeOsBvgoz5Fqxwbc4JHep18AHEZC2.IEJDOcTvkKDbj_9QQ4ntUCqeQJ4PQJe";
						configure.UseNativeClientAuthentication(clientId, listenerTcpPort: 7890);
					})
					.AddForceMetadataClient(configure =>
					{
						var clientId = "3MVG9A_f29uWoVQvrJSnfk5LPeOsBvgoz5Fqxwbc4JHep18AHEZC2.IEJDOcTvkKDbj_9QQ4ntUCqeQJ4PQJe";
						configure.UseNativeClientAuthentication(clientId, listenerTcpPort: 7890);
					})
					.AddForceMetadataProcesses()
					.BuildServiceProvider();

			const OrgType orgType = OrgType.Production;
			const string username = "daniel.stolt@astratech.com";

			var client = services.GetRequiredService<IMetadataClientFactory>().CreateClient(orgType, username);
			var retrieveProcess = services.GetRequiredService<RetrieveProcess>();

			var jsonSettings = new JsonSerializerSettings();
			jsonSettings.Formatting = Formatting.Indented;
			jsonSettings.Converters.Add(new StringEnumConverter());

			try
			{
				Console.WriteLine("Describing metadata...");
				var metadataDescription = await client.DescribeAsync();

				//Console.WriteLine("Loading previous archive...");
				//var storage1 = new DirectoryArchiveStorage(@"C:\Temp\Delta\PreviousArchive", services.GetService<ILogger<DirectoryArchiveStorage>>());
				//var archive1 = await Archive.LoadAsync(storage1, metadataDescription);

				//Console.WriteLine("Loading current archive...");
				//var storage2 = new DirectoryArchiveStorage(@"C:\Temp\Delta\CurrentArchive", services.GetService<ILogger<DirectoryArchiveStorage>>());
				//var archive2 = await Archive.LoadAsync(storage2, metadataDescription);

				//Console.WriteLine("Creating delta archive...");
				//if (Directory.Exists(@"C:\Temp\Delta\DeltaArchive"))
				//    Directory.Delete(@"C:\Temp\Delta\DeltaArchive", recursive: true);
				//Directory.CreateDirectory(@"C:\Temp\Delta\DeltaArchive");
				//var storage3 = new DirectoryArchiveStorage(@"C:\Temp\Delta\DeltaArchive", services.GetService<ILogger<DirectoryArchiveStorage>>());
				//var archive3 = new Archive(storage3, metadataDescription, isSinglePackage: false);
				//await archive2.WriteDeltaSinceAsync(archive1, archive3);

				//var metadataDescriptionJson = JsonConvert.SerializeObject(metadataDescription, jsonSettings);
				//await File.WriteAllTextAsync(@"C:\Temp\MetadataDescription.json", metadataDescriptionJson);

				//Console.WriteLine("Describing type CustomObject...");
				//var objectDescription = await client.DescribeTypeAsync("CustomObject");
				//var objectDescriptionJson = JsonConvert.SerializeObject(objectDescription, jsonSettings);
				//await File.WriteAllTextAsync(@"C:\Temp\CustomObjectDescription.json", objectDescriptionJson);

				//Console.WriteLine("Describing type CustomField...");
				//var fieldDescription = await client.DescribeTypeAsync("CustomField");
				//var fieldDescriptionJson = JsonConvert.SerializeObject(fieldDescription, jsonSettings);
				//await File.WriteAllTextAsync(@"C:\Temp\CustomFieldDescription.json", fieldDescriptionJson);

				Console.WriteLine("Listing metadata items...");
				var metadataTypeNames = metadataDescription.Types.Keys;
				var itemInfoList = await retrieveProcess.ListItemsOfTypesAsync(orgType, username, metadataTypeNames, includePackages: true);
				Console.WriteLine($"{itemInfoList.Count()} metadata items found.");
				var itemInfoListJson = JsonConvert.SerializeObject(itemInfoList, jsonSettings);
				await File.WriteAllTextAsync(@"C:\Temp\MetadataList.json", itemInfoListJson);

				Console.WriteLine("Packages found:");
				var packageNames =
					itemInfoList
						.Where(x => x.Type == "InstalledPackage")
						.Select(x => x.Name)
						.Distinct();
				foreach (var packageName in packageNames)
					Console.WriteLine($"  {packageName}");

				//var operationId = await client.StartRetrieveAsync(new[] { new MetadataRetrieveQuery("CustomObject", "*") });
				//RetrieveResult result;
				//while (!(result = await client.GetRetrieveResultAsync(operationId)).IsDone)
				//    await Task.Delay(TimeSpan.FromSeconds(3));

				//await File.WriteAllBytesAsync(@"C:\Temp\Metadata.zip", result.ZipFile);

				//var storage = new DirectoryArchiveStorage(@"C:\Temp\Metadata", services.GetService<ILogger<DirectoryArchiveStorage>>());

				//var package = new Package(storage, metadataDescription, "Geopointe", "Geopointe");
				//var components = await package.GetComponentsAsync();
				//var componentsJson = JsonConvert.SerializeObject(components, jsonSettings);
				//await File.WriteAllTextAsync(@"C:\Temp\GeopointeComponents.json", componentsJson);
				//await package.WriteManifestAsync();

				//package = new Package(storage, metadataDescription, null, "unpackaged");
				//components = await package.GetComponentsAsync();
				//componentsJson = JsonConvert.SerializeObject(components, jsonSettings);
				//await File.WriteAllTextAsync(@"C:\Temp\UnpackagedComponents.json", componentsJson);
				//await package.WriteManifestAsync();

				//logger.LogInformation("Retrieving metadata...");

				//var result = await retrieveWorker.RetrieveAsync(itemReferences, $"C:\\Temp\\Metadata-{Guid.NewGuid()}");
				//var missingQuery =
				//    from resultItem in result
				//    where resultItem.Value == false
				//    orderby resultItem.Key.Type, resultItem.Key.FullName
				//    select resultItem.Key;
				//foreach (var itemReference in missingQuery)
				//    logger.LogWarning($"MISSING: {itemReference.Type} {itemReference.FullName}");
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error while listing metadata.");
				Console.WriteLine(ex.Message);
			}

			Console.WriteLine("Press any key to continue...");
			Console.ReadKey();
		}
	}
}
