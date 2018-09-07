using System;
using System.Threading.Tasks;
using IdentityModel.OidcClient;

namespace IDeliverable.ForceClient.Tools.Metadata
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var options = new OidcClientOptions
            {
                Authority = "https://login.salesforce.com/",
                ClientId = "3MVG9A_f29uWoVQvrJSnfk5LPeA2zP4q_U4piOC.9D9E0xzbHOmSZJYroajSEEGlK32K_X9i66uunCW3BBCnE",
                RedirectUri = "http://localhost:7890/",
                //Scope = "api refresh_token offline_access",
                Flow = OidcClientOptions.AuthenticationFlow.AuthorizationCode,
                ResponseMode = OidcClientOptions.AuthorizeResponseMode.Redirect,
                Browser = new SystemBrowser(port: 7890)
            };

            var client = new OidcClient(options);

            var result = await client.LoginAsync();
            var token = result.AccessToken;

            Console.WriteLine(token);
        }
    }
}
