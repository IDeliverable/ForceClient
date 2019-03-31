using System;
using System.Threading.Tasks;
using IDeliverable.ForceClient.Core;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nito.AsyncEx;

namespace IDeliverable.ForceClient.Tools.Metadata.Authentication
{
    public class BrowserOrgAccessProvider : IOrgAccessProvider
    {
        public BrowserOrgAccessProvider(OrgType orgType, string clientId, string clientSecret, int redirectTcpPort = 7890)
        {
            string authority;
            switch (orgType)
            {
                case OrgType.Production:
                    authority = Constants.AuthorizationEndpointUrlProduction;
                    break;
                case OrgType.Sandbox:
                    authority = Constants.AuthorizationEndpointUrlSandbox;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orgType), $"Unrecognized {nameof(OrgType)} value '{orgType}'.");
            }

            mBrowser = new SystemBrowser(port: 7890);
            mLoginResultLazy = new AsyncLazy<LoginResult>(AuthenticateAsync);
            mOptions = new OidcClientOptions()
            {
                Authority = authority,
                ClientId = clientId,
                // TODO: Figure out how to do this without a client secret!
                ClientSecret = clientSecret,
                RedirectUri = $"http://localhost:{redirectTcpPort}/",
                Flow = OidcClientOptions.AuthenticationFlow.AuthorizationCode,
                ResponseMode = OidcClientOptions.AuthorizeResponseMode.Redirect,
                Browser = mBrowser
            };
        }

        private readonly IBrowser mBrowser;
        private readonly AsyncLazy<LoginResult> mLoginResultLazy;
        private readonly OidcClientOptions mOptions;

        public async Task<string> GetSoapUrlAsync()
        {
            var loginResult = await mLoginResultLazy;
            var urlsJson = loginResult.User.FindFirst("urls").Value;
            var urls = JsonConvert.DeserializeObject(urlsJson) as JObject;
            var metadataUrl = urls["metadata"].ToString();
            return metadataUrl;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            var loginResult = await mLoginResultLazy;
            var accessToken = loginResult.AccessToken;
            return accessToken;
        }

        private async Task<LoginResult> AuthenticateAsync()
        {
            var client = new OidcClient(mOptions);
            var request = new LoginRequest();
            var loginResult = await client.LoginAsync(request);
            return loginResult;
        }
    }
}
