using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IDeliverable.ForceClient.Core;
using IDeliverable.ForceClient.Metadata;
using IDeliverable.ForceClient.Metadata.Client;
using IDeliverable.ForceClient.Metadata.Retrieve;
using IdentityModel.OidcClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IDeliverable.ForceClient.Tools.Metadata
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole(LogLevel.Debug, includeScopes: true);
            var logger = loggerFactory.CreateLogger<Program>();

            try
            {
                logger.LogInformation("Acquiring access token...");

                var options = new OidcClientOptions
                {
                    Authority = Constants.AuthorizationEndpointUrlProduction,
                    ClientId = "3MVG9A_f29uWoVQvrJSnfk5LPeA2zP4q_U4piOC.9D9E0xzbHOmSZJYroajSEEGlK32K_X9i66uunCW3BBCnE",
                    ClientSecret = "2763444084747273086",
                    RedirectUri = "http://localhost:7890/",
                    Flow = OidcClientOptions.AuthenticationFlow.AuthorizationCode,
                    ResponseMode = OidcClientOptions.AuthorizeResponseMode.Redirect,
                    Browser = new SystemBrowser(port: 7890)
                };

                var client = new OidcClient(options);
                var request = new LoginRequest();
                var loginResult = await client.LoginAsync(request);

                var accessToken = loginResult.AccessToken;
                var urlsJson = loginResult.User.FindFirst("urls").Value;
                var urls = JsonConvert.DeserializeObject(urlsJson) as JObject;
                var metadataUrl = urls["metadata"].ToString();

                var metadataRules = new MetadataRules();
                var metadataGatewayLogger = loggerFactory.CreateLogger<SoapMetadataClient>();
                var metadataGateway = new SoapMetadataClient(metadataUrl, accessToken, metadataRules, metadataGatewayLogger);
                var retrieveWorkerLogger = loggerFactory.CreateLogger<RetrieveWorker>();
                var retrieveWorker = new RetrieveWorker(metadataGateway, metadataRules, retrieveWorkerLogger);

                logger.LogInformation("Listing metadata items...");

                var metadataTypes = metadataRules.GetSupportedTypes();
                var itemInfoList = await retrieveWorker.ListItemsAsync(metadataTypes);

                logger.LogInformation($"{itemInfoList.Count()} items found.");

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
                logger.LogError(ex, "Error while retrieving metadata.");
            }
        }
    }
}
