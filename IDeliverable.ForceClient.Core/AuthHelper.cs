using System;
using System.Threading.Tasks;

namespace IDeliverable.ForceClient.Core
{
    public class AuthHelper
    {
        private const string mRedirectUrl = "sfdc://success";

        public static Task<TokenInfo> DoAuth(OrgType orgType, string consumerKey)
        {
            var requestUri = new Uri($"{Constants.AuthorizationEndpointUrlProduction}?response_type={AuthResponseType.Token.ToString().ToLower()}&client_id={consumerKey}&redirect_uri={mRedirectUrl}&display={AuthDisplayType.Touch.ToString().ToLower()}");
            var callbackUri = new Uri(mRedirectUrl);

            throw new NotImplementedException();

            //var result = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, requestUri, callbackUri);

            //switch (result.ResponseStatus)
            //{
            //    case WebAuthenticationStatus.Success:
            //        return TokenInfo.FromResponse(result.ResponseData);

            //    case WebAuthenticationStatus.UserCancel:
            //        throw new Exception("The authentication flow was cancelled by the user.");

            //    case WebAuthenticationStatus.ErrorHttp:
            //        throw new Exception($"An HTTP error with result code {result.ResponseErrorDetail} occurred during the authentication flow.");

            //    default:
            //        throw new Exception("An unsupported enum value was detected.");
            //}
        }
    }
}
