using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IDeliverable.ForceClient.Core;
using IDeliverable.ForceClient.Metadata;
using IDeliverable.ForceClient.Metadata.Client;
using IDeliverable.ForceClient.Metadata.Retrieve;
using IDeliverable.ForceClient.Tools.Metadata.Authentication;
using Microsoft.Extensions.DependencyInjection;
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
                    .BuildServiceProvider();

            // TODO: Figure out how to do this without a client secret!
            var clientId = "3MVG9A_f29uWoVQvrJSnfk5LPeA2zP4q_U4piOC.9D9E0xzbHOmSZJYroajSEEGlK32K_X9i66uunCW3BBCnE";
            var clientSecret = "2763444084747273086";

            var orgAccessProvider = new BrowserOrgAccessProvider(OrgType.Production, clientId, clientSecret);
            var retrieveWorkerFactory = services.GetRequiredService<IRetrieveWorkerFactory>();
            var metadataRules = services.GetRequiredService<MetadataRules>();
            var retrieveWorker = retrieveWorkerFactory.CreateRetrieveWorker(orgAccessProvider);

            var client = services.GetRequiredService<IMetadataClientFactory>().CreateClient(orgAccessProvider);

            var jsonSettings = new JsonSerializerSettings();
            jsonSettings.Formatting = Formatting.Indented;
            jsonSettings.Converters.Add(new StringEnumConverter());

            try
            {
                Console.WriteLine("Describing metadata...");
                var metadataDescription = await client.DescribeAsync();
                var metadataDescriptionJson = JsonConvert.SerializeObject(metadataDescription, jsonSettings);
                await File.WriteAllTextAsync(@"C:\Temp\MetadataDescription.json", metadataDescriptionJson);

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

                //var operationId = await client.StartRetrieveAsync(new[] { new MetadataRetrieveSpec(MetadataType.CustomObject, "*") });
                //RetrieveResult result;
                //while (!(result = await client.GetRetrieveResultAsync(operationId)).IsDone)
                //    await Task.Delay(TimeSpan.FromSeconds(3));

                //await File.WriteAllBytesAsync(@"C:\Temp\Metadata.zip", result.ZipFile);

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
