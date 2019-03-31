using System;
using System.Linq;
using System.Threading.Tasks;
using IDeliverable.ForceClient.Metadata;
using IDeliverable.ForceClient.Metadata.Retrieve;
using IDeliverable.ForceClient.Tools.Metadata.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace IDeliverable.ForceClient.Tools.Metadata
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger =
                new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("IDeliverable.ForceClient.Metadata.Client", LogEventLevel.Fatal)
                    .WriteTo.Console()
                    .CreateLogger();

            var services =
                new ServiceCollection()
                    .AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true))
                    .AddMetadataServices()
                    .BuildServiceProvider();

            var orgAccessProvider = new BrowserOrgAccessProvider();
            var retrieveWorkerFactory = services.GetRequiredService<IRetrieveWorkerFactory>();
            var metadataRules = services.GetRequiredService<MetadataRules>();
            var retrieveWorker = retrieveWorkerFactory.CreateRetrieveWorker(orgAccessProvider);

            try
            {
                Console.WriteLine("Listing metadata items...");
                var metadataTypes = metadataRules.GetSupportedTypes();
                var itemInfoList = await retrieveWorker.ListItemsAsync(metadataTypes);
                Console.WriteLine($"{itemInfoList.Count()} items found.");

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
