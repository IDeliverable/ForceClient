using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IDeliverable.ForceClient.Core;
using IDeliverable.ForceClient.Core.OrgAccess;
using IDeliverable.ForceClient.Core.Tokens;
using IdentityModel.OidcClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace IDeliverable.ForceClient.Tools.Metadata.Authentication
{
    public class BrowserOrgAccessProvider : IOrgAccessProvider
    {
        public BrowserOrgAccessProvider(ITokenStore tokenStore, ILogger<BrowserOrgAccessProvider> logger, OrgType orgType, string username, string clientId, string clientSecret, int redirectTcpPort = 7890)
        {
            mTokenStore = tokenStore;
            mLogger = logger;
            mOrgType = orgType;
            mUsername = username;

            switch (orgType)
            {
                case OrgType.Production:
                    mAuthority = Constants.AuthorizationEndpointUrlProduction;
                    break;
                case OrgType.Sandbox:
                    mAuthority = Constants.AuthorizationEndpointUrlSandbox;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orgType), $"Unrecognized {nameof(OrgType)} value '{orgType}'.");
            }

            mOidcOptions = new OidcClientOptions()
            {
                Authority = mAuthority,
                ClientId = clientId,
                // TODO: Figure out how to do this without a client secret!
                ClientSecret = clientSecret,
                RedirectUri = $"http://localhost:{redirectTcpPort}/",
                Flow = OidcClientOptions.AuthenticationFlow.AuthorizationCode,
                ResponseMode = OidcClientOptions.AuthorizeResponseMode.Redirect,
                Browser = new SystemBrowser(port: 7890)
            };
        }

        private readonly ITokenStore mTokenStore;
        private readonly ILogger mLogger;
        private readonly OrgType mOrgType;
        private readonly string mUsername;
        private readonly string mAuthority;
        private readonly OidcClientOptions mOidcOptions;

        public async Task<string> GetSoapApiUrlAsync(string apiName)
        {
            var urls = await mTokenStore.LoadUrlsAsync(mOrgType, mUsername);
            if (urls == null)
            {
                mLogger.LogDebug($"No URLs found in store for '{mUsername}'.");

                // Since we have to stored URLs, and URLs are not returned when using a refresh token (only on
                // full interactive login flow), we need to also clear out any saved refresh tokens to force
                // a full login, so that we may acquire the URLs.
                await mTokenStore.DeleteTokenAsync(TokenKind.RefreshToken, mOrgType, mUsername);

                var client = new OidcClient(mOidcOptions);
                var tokenData = await AcquireAccessTokenAsync(client);
                urls = tokenData.Urls;
            }

            return urls[apiName];
        }

        public async Task<string> GetAccessTokenAsync(bool forceRefresh)
        {
            string accessToken = null;

            if (!forceRefresh)
                accessToken = await mTokenStore.LoadTokenAsync(TokenKind.AccessToken, mOrgType, mUsername);

            if (accessToken == null)
            {
                mLogger.LogDebug($"No access token found in store for '{mUsername}'.");

                var client = new OidcClient(mOidcOptions);
                var tokenData = await AcquireAccessTokenAsync(client);
                accessToken = tokenData.AccessToken;
            }

            return accessToken;
        }

        private async Task<TokenData> AcquireAccessTokenAsync(OidcClient client)
        {
            mLogger.LogDebug($"Acquiring access token for '{mUsername}'...");

            // If we have a refresh token, first try to use that to acquire an access token.
            var refreshToken = await mTokenStore.LoadTokenAsync(TokenKind.RefreshToken, mOrgType, mUsername);
            if (refreshToken != null)
            {
                mLogger.LogDebug($"Refresh token found in store store for '{mUsername}'.");

                var tokenData = await RefreshAccessTokenAsync(client, refreshToken, throwOnError: false);
                if (tokenData != null)
                    return tokenData;
            }

            mLogger.LogDebug($"Refresh token could not be used for '{mUsername}'; authenticating...");

            // Either we did not have any refresh token, or using it failed; perform a full interactive login flow.
            var request = new LoginRequest();
            var loginResult = await client.LoginAsync(request);
            if (loginResult.IsError)
                throw new Exception($"Error during authentication: {loginResult.Error}");

            mLogger.LogDebug($"Successfully authenticated '{mUsername}'.");

            var urlsJson = loginResult.User.FindFirst("urls").Value;
            var urls = JsonConvert.DeserializeObject<Dictionary<string, string>>(urlsJson);

            await mTokenStore.SaveTokenAsync(TokenKind.AccessToken, mOrgType, mUsername, loginResult.AccessToken);
            await mTokenStore.SaveTokenAsync(TokenKind.RefreshToken, mOrgType, mUsername, loginResult.RefreshToken);
            await mTokenStore.SaveUrlsAsync(mOrgType, mUsername, urls);

            var result =
                new TokenData(
                    loginResult.AccessToken,
                    loginResult.RefreshToken,
                    urls);

            return result;
        }

        private async Task<TokenData> RefreshAccessTokenAsync(OidcClient client, string refreshToken, bool throwOnError)
        {
            mLogger.LogDebug($"Refreshing access token for '{mUsername}'...");

            var refreshTokenResult = await client.RefreshTokenAsync(refreshToken);

            if (refreshTokenResult.IsError)
            {
                if (throwOnError)
                    throw new Exception($"Error during access token refresh: {refreshTokenResult.Error}");

                mLogger.LogDebug($"Failed to refresh access token for '{mUsername}'. Error: {refreshTokenResult.Error}");

                return null;
            }

            mLogger.LogDebug($"Successfully refreshed access token for '{mUsername}'.");

            await mTokenStore.SaveTokenAsync(TokenKind.AccessToken, mOrgType, mUsername, refreshTokenResult.AccessToken);
            await mTokenStore.SaveTokenAsync(TokenKind.RefreshToken, mOrgType, mUsername, refreshTokenResult.RefreshToken);

            var result =
                new TokenData(
                    refreshTokenResult.AccessToken,
                    refreshTokenResult.RefreshToken,
                    urls: null);

            return result;
        }

        private class TokenData
        {
            public TokenData(string accessToken, string refreshToken, IReadOnlyDictionary<string, string> urls)
            {
                AccessToken = accessToken;
                RefreshToken = refreshToken;
                Urls = urls;
            }

            public string AccessToken { get; }
            public string RefreshToken { get; }
            public IReadOnlyDictionary<string, string> Urls { get; }
        }
    }
}
