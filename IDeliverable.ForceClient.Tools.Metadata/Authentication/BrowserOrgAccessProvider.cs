using System;
using System.Threading.Tasks;
using IDeliverable.ForceClient.Core;
using IDeliverable.ForceClient.Metadata.Client;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IDeliverable.ForceClient.Tools.Metadata.Authentication
{
    public class BrowserOrgAccessProvider : IOrgAccessProvider
    {
        public BrowserOrgAccessProvider()
        {
            mBrowser = new SystemBrowser(port: 7890);
            mLoginResultLazy = new AsyncLazy<LoginResult>(AuthenticateAsync);
        }

        private readonly IBrowser mBrowser;
        private readonly AsyncLazy<LoginResult> mLoginResultLazy;

        public async Task<string> GetSoapUrlAsync()
        {
            var loginResult = await mLoginResultLazy.Value;
            var urlsJson = loginResult.User.FindFirst("urls").Value;
            var urls = JsonConvert.DeserializeObject(urlsJson) as JObject;
            var metadataUrl = urls["metadata"].ToString();
            return metadataUrl;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            var loginResult = await mLoginResultLazy.Value;
            var accessToken = loginResult.AccessToken;
            return accessToken;
        }

        private async Task<LoginResult> AuthenticateAsync()
        {
            var options = new OidcClientOptions()
            {
                Authority = Constants.AuthorizationEndpointUrlProduction,
                ClientId = "3MVG9A_f29uWoVQvrJSnfk5LPeA2zP4q_U4piOC.9D9E0xzbHOmSZJYroajSEEGlK32K_X9i66uunCW3BBCnE",
                // TODO: Figure out how to do this without a client secret!
                ClientSecret = "2763444084747273086",
                RedirectUri = "http://localhost:7890/",
                Flow = OidcClientOptions.AuthenticationFlow.AuthorizationCode,
                ResponseMode = OidcClientOptions.AuthorizeResponseMode.Redirect,
                Browser = mBrowser
            };

            var client = new OidcClient(options);
            var request = new LoginRequest();

            var loginResult = await client.LoginAsync(request);

            return loginResult;
        }

        private class AsyncLazy<T> : Lazy<Task<T>>
        {
            public AsyncLazy(Func<T> valueFactory)
                : base(() => Task.Factory.StartNew(valueFactory))
            { }

            public AsyncLazy(Func<Task<T>> taskFactory)
                : base(() => Task.Factory.StartNew(() => taskFactory()).Unwrap())
            { }
        }
    }
}
