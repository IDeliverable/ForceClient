using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IDeliverable.ForceClient.Core.Tokens;
using IdentityModel.OidcClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace IDeliverable.ForceClient.Core.OrgAccess.Native
{
	public class NativeClientOrgAccessProvider : IOrgAccessProvider
	{
		public NativeClientOrgAccessProvider(ITokenStore tokenStore, ILogger<NativeClientOrgAccessProvider> logger, string clientId, int listenerTcpPort = 7890)
		{
			mTokenStore = tokenStore;
			mLogger = logger;
			mClientId = clientId;
			mListenerTcpPort = listenerTcpPort;
		}

		private readonly ITokenStore mTokenStore;
		private readonly ILogger mLogger;
		private readonly string mClientId;
		private readonly int mListenerTcpPort;

		public async Task<string> GetSoapApiUrlAsync(OrgType orgType, string username, string apiName)
		{
			var urls = await mTokenStore.LoadUrlsAsync(orgType, username);
			if (urls == null)
			{
				mLogger.LogDebug($"No URLs found in store for user '{username}'.");

				// Since we have to stored URLs, and URLs are not returned when using a refresh token (only on
				// full interactive login flow), we need to also clear out any saved refresh tokens to force
				// a full login, so that we may acquire the URLs.
				await mTokenStore.DeleteTokenAsync(TokenKind.RefreshToken, orgType, username);

				var tokenData = await AcquireAccessTokenAsync(orgType, username);
				urls = tokenData.Urls;
			}

			return urls[apiName];
		}

		public async Task<string> GetAccessTokenAsync(OrgType orgType, string username, bool forceRefresh)
		{
			string accessToken = null;

			if (!forceRefresh)
				accessToken = await mTokenStore.LoadTokenAsync(TokenKind.AccessToken, orgType, username);

			if (accessToken == null)
			{
				mLogger.LogDebug($"No access token found in store for user '{username}'.");

				var tokenData = await AcquireAccessTokenAsync(orgType, username);
				accessToken = tokenData.AccessToken;
			}

			return accessToken;
		}

		private OidcClient CreateClient(OrgType orgType)
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

			var options = new OidcClientOptions()
			{
				Authority = authority,
				ClientId = mClientId,
				RedirectUri = $"http://localhost:{mListenerTcpPort}/",
				Flow = OidcClientOptions.AuthenticationFlow.AuthorizationCode,
				ResponseMode = OidcClientOptions.AuthorizeResponseMode.Redirect,
				Browser = new DesktopClientBrowser(port: 7890)
			};

			return new OidcClient(options);
		}

		private async Task<TokenData> AcquireAccessTokenAsync(OrgType orgType, string username)
		{
			mLogger.LogDebug($"Acquiring access token for user '{username}'...");

			// If we have a refresh token, first try to use that to acquire an access token.
			var refreshToken = await mTokenStore.LoadTokenAsync(TokenKind.RefreshToken, orgType, username);
			if (!String.IsNullOrEmpty(refreshToken))
			{
				mLogger.LogDebug($"Refresh token found in store store for user '{username}'.");

				var tokenData = await RefreshAccessTokenAsync(orgType, username, refreshToken, throwOnError: false);
				if (tokenData != null)
					return tokenData;
			}

			mLogger.LogDebug($"Refresh token could not be used for user '{username}'; authorizing...");

			// Either we did not have any refresh token, or using it failed; perform a full interactive login flow.
			var client = CreateClient(orgType);
			var request = new LoginRequest()
			{
				FrontChannelExtraParameters = new Dictionary<string, string>()
				{
					{ "login_hint", username }, // Prepopulate the username field
					{ "prompt", "login" } // Force re-authentication even if browser is already signed in
				}
			};
			var loginResult = await client.LoginAsync(request);
			if (loginResult.IsError)
				throw new Exception($"Error during authorization: {loginResult.Error}");

			mLogger.LogDebug($"Successfully acquired access token for user '{username}'.");

			var urlsJson = loginResult.User.FindFirst("urls").Value;
			var urls = JsonConvert.DeserializeObject<Dictionary<string, string>>(urlsJson);

			await mTokenStore.SaveTokenAsync(TokenKind.AccessToken, orgType, username, loginResult.AccessToken);
			await mTokenStore.SaveTokenAsync(TokenKind.RefreshToken, orgType, username, loginResult.RefreshToken);
			await mTokenStore.SaveUrlsAsync(orgType, username, urls);

			var result =
				new TokenData(
					loginResult.AccessToken,
					loginResult.RefreshToken,
					urls);

			return result;
		}

		private async Task<TokenData> RefreshAccessTokenAsync(OrgType orgType, string username, string refreshToken, bool throwOnError)
		{
			mLogger.LogDebug($"Refreshing access token for user '{username}'...");

			var client = CreateClient(orgType);
			var refreshTokenResult = await client.RefreshTokenAsync(refreshToken);

			if (refreshTokenResult.IsError)
			{
				if (throwOnError)
					throw new Exception($"Error during access token refresh: {refreshTokenResult.Error}");

				mLogger.LogDebug($"Failed to refresh access token for user '{username}'. Error: {refreshTokenResult.Error}");

				return null;
			}

			mLogger.LogDebug($"Successfully refreshed access token for user '{username}'.");

			await mTokenStore.SaveTokenAsync(TokenKind.AccessToken, orgType, username, refreshTokenResult.AccessToken);
			await mTokenStore.SaveTokenAsync(TokenKind.RefreshToken, orgType, username, refreshTokenResult.RefreshToken);

			var result =
				new TokenData(
					refreshTokenResult.AccessToken,
					refreshTokenResult.RefreshToken,
					urls: null);

			return result;
		}

		class TokenData
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
