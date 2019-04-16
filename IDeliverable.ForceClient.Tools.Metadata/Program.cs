using System;
using System.IO;
using System.Threading.Tasks;
using IDeliverable.ForceClient.Core;
using IDeliverable.ForceClient.Metadata;
using IDeliverable.ForceClient.Metadata.Archives;
using IDeliverable.ForceClient.Metadata.Archives.Storage;
using IDeliverable.ForceClient.Metadata.Client;
using IDeliverable.ForceClient.Metadata.Processes.Retrieve;
using IDeliverable.ForceClient.Tools.Metadata.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;

namespace IDeliverable.ForceClient.Tools.Metadata
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger =
                new LoggerConfiguration()
                    .MinimumLevel.Information()
                    //.MinimumLevel.Override("IDeliverable.ForceClient.Metadata.Client", LogEventLevel.Fatal)
                    .WriteTo.Console()
                    .CreateLogger();

            var services =
                new ServiceCollection()
                    .AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true))
                    .AddMetadataServices()
                    .AddMetadataProcesses()
                    .BuildServiceProvider();

            // TODO: Figure out how to do this without a client secret!
            var clientId = "3MVG9A_f29uWoVQvrJSnfk5LPeA2zP4q_U4piOC.9D9E0xzbHOmSZJYroajSEEGlK32K_X9i66uunCW3BBCnE";
            var clientSecret = "2763444084747273086";

            var orgAccessProvider = new BrowserOrgAccessProvider(OrgType.Production, clientId, clientSecret);
            var retrieveProcessFactory = services.GetRequiredService<IRetrieveProcessFactory>();
            var metadataRules = services.GetRequiredService<MetadataRules>();
            var retrieveProcess = retrieveProcessFactory.CreateRetrieveProcess(orgAccessProvider);

            var client = services.GetRequiredService<IMetadataClientFactory>().CreateClient(orgAccessProvider);

            var jsonSettings = new JsonSerializerSettings();
            jsonSettings.Formatting = Formatting.Indented;
            jsonSettings.Converters.Add(new StringEnumConverter());

            try
            {
                Console.WriteLine("Describing metadata...");
                var metadataDescription = await client.DescribeAsync();

                Console.WriteLine("Loading previous archive...");
                var storage1 = new DirectoryArchiveStorage(@"C:\Temp\Delta\PreviousArchive", services.GetService<ILogger<DirectoryArchiveStorage>>());
                var archive1 = await Archive.LoadAsync(storage1, metadataDescription);

                Console.WriteLine("Loading current archive...");
                var storage2 = new DirectoryArchiveStorage(@"C:\Temp\Delta\CurrentArchive", services.GetService<ILogger<DirectoryArchiveStorage>>());
                var archive2 = await Archive.LoadAsync(storage2, metadataDescription);

                Console.WriteLine("Creating delta archive...");
                if (Directory.Exists(@"C:\Temp\Delta\DeltaArchive"))
                    Directory.Delete(@"C:\Temp\Delta\DeltaArchive", recursive: true);
                Directory.CreateDirectory(@"C:\Temp\Delta\DeltaArchive");
                var storage3 = new DirectoryArchiveStorage(@"C:\Temp\Delta\DeltaArchive", services.GetService<ILogger<DirectoryArchiveStorage>>());
                var archive3 = new Archive(storage3, metadataDescription, isSinglePackage: false);
                await archive2.WriteDeltaSinceAsync(archive1, archive3);

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

                //Console.WriteLine("Listing metadata items...");
                //var metadataTypeNames = metadataDescription.Types.Keys;
                //var itemInfoList = await retrieveWorker.ListItemsAsync(metadataTypeNames);
                //Console.WriteLine($"{itemInfoList.Count()} items found.");
                //var itemInfoListJson = JsonConvert.SerializeObject(itemInfoList, jsonSettings);
                //await File.WriteAllTextAsync(@"C:\Temp\MetadataList.json", itemInfoListJson);

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
        }
    }
}
