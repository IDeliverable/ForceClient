using System.IO;
using System.Threading.Tasks;
using IDeliverable.ForceClient.Core;
using IDeliverable.ForceClient.Metadata;
using IDeliverable.ForceClient.Metadata.Retrieve;
using IdentityModel.OidcClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IDeliverable.ForceClient.Tools.Metadata
{
    class Program
    {
        static async Task Main(string[] args)
        {
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
            var result = await client.LoginAsync(request);

            var accessToken = result.AccessToken;
            var urlsJson = result.User.FindFirst("urls").Value;
            var urls = JsonConvert.DeserializeObject(urlsJson) as JObject;
            var metadataUrl = urls["metadata"].ToString();

            var metadataGateway = new MetadataGateway(metadataUrl, accessToken);
            var retrieveWorker = new RetrieveWorker(metadataGateway);

            using (var fileStream = File.Create(@"C:\Temp\Metadata.zip"))
            {
                await retrieveWorker.RetrieveAllAsync(new[] { MetadataType.CustomObject }, fileStream);
            }
        }
    }
}
