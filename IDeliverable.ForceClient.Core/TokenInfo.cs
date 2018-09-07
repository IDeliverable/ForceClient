using System;
using Microsoft.AspNetCore.WebUtilities;

namespace IDeliverable.ForceClient.Core
{
    public class TokenInfo
    {
        public static TokenInfo FromResponse(string responseData)
        {
            var responseUri = new Uri(responseData);
            var query = QueryHelpers.ParseQuery(responseUri.Fragment.Replace("#", "?"));

            // TODO: Parse in a more dynamic way.

            return new TokenInfo(
                query["access_token"],
                TimeSpan.Zero, // TimeSpan.FromSeconds(Int32.Parse(decoder.GetFirstValueByName("expires_in"))),
                query["token_type"],
                query["refresh_token"],
                query["scope"].ToString().Split(' '),
                null, //decoder.GetFirstValueByName("state"),
                new Uri(query["instance_url"]),
                new Uri(query["id"]),
                new DateTime(1970, 01, 01, 0, 0, 0, DateTimeKind.Utc) + TimeSpan.FromMilliseconds(Double.Parse(query["issued_at"])), // Time represented as the number of milliseconds since the Unix epoch (00:00:00 UTC on 1 January 1970).
                query["signature"]
            );
        }

        public TokenInfo(string accessToken, TimeSpan accessTokenExpiresIn, string accessTokenType, string refreshToken, string[] scope, string state, Uri instanceUrl, Uri identityUrl, DateTime issuedAt, string signatureBase64)
        {
            AccessToken = accessToken;
            AccessTokenExpiresIn = accessTokenExpiresIn;
            AccessTokenType = accessTokenType;
            RefreshToken = refreshToken;
            Scope = scope;
            State = state;
            InstanceUrl = instanceUrl;
            IdentityUrl = identityUrl;
            IssuedAt = issuedAt;
            SignatureBase64 = signatureBase64;
        }

        public string AccessToken { get; }
        public TimeSpan AccessTokenExpiresIn { get; }
        public string AccessTokenType { get; }
        public string RefreshToken { get; }
        public string[] Scope { get; }
        public string State { get; }
        public Uri InstanceUrl { get; }
        public Uri IdentityUrl { get; }
        public DateTime IssuedAt { get; }
        public string SignatureBase64 { get; }
    }
}
