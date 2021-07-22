using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Identity.Client;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Sales.MSALTokenGenerator;

namespace Microsoft.Sales.Leads.Common.Helper
{
    [ExcludeFromCodeCoverage]
    public static class TokenGeneratorUsingMsalLibrary
    {
        private static IConfidentialClientApplication _confidentialClientApp;
        private static AuthenticationResult _authenticationResult;
        private static IPublicClientApplication _publicClientApp;
        private static string accessToken = string.Empty;

        /// <summary>
        /// This will generate token for Confidential client apps 
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="authority"></param>
        /// <param name="scopes"></param>
        /// <returns>bearer token</returns>
        public static async Task<string> ConfidentialClientBuilderApp(string resource = null, string clientId = null,
            string clientSecret = null, string authority = null, IEnumerable<string> scopes = null)
        {
                // We will use MSAL.NET to get a token to call the API On Behalf Of the current user
                try
                {
                    // Creating a ConfidentialClientApplication using the Build pattern (https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Client-Applications)
                    ExponentialRetryStrategy retryStrategy = new ExponentialRetryStrategy();

                    _confidentialClientApp = ConfidentialClientApplicationBuilder.Create(clientId)
                        .WithAuthority(String.Format(CultureInfo.InvariantCulture, authority))
                        .WithClientSecret(clientSecret).WithLegacyCacheCompatibility(false)
                        .Build();

                    // In memory distributed token cache
                    //_confidentialClientApp.AddInMemoryTokenCache();

                    // Acquiring an AuthenticationResult for the scope user.read, impersonating the user represented by userAssertion, using the OBO flow
                    await RetryHelper.RetryAsync(async () =>
                    {
                        _authenticationResult = await _confidentialClientApp.AcquireTokenForClient(scopes).ExecuteAsync().ConfigureAwait(false);
                    }, (exception) =>
                    {
                        return true;
                    }, retryStrategy.ShouldRetry(), new Action<Exception, RetryErrorType, int>(LogTokenGenerationFailure)).ConfigureAwait(false);

                    if (_authenticationResult != null && !string.IsNullOrEmpty(_authenticationResult.AccessToken))
                        accessToken = _authenticationResult.AccessToken;

                    if (accessToken == null)
                    {
                        throw new Exception("Access Token could not be acquired.");
                    }

                    return accessToken;
                }
                catch (MsalUiRequiredException)
                {
                    /*
                    * If you used the scope `.default` on the client application, the user would have been prompted to consent for Graph API back there
                    * and no incremental consents are required (this exception is not expected). However, if you are using the scope `access_as_user`,
                    * this exception will be thrown at the first time the API tries to access Graph on behalf of the user for an incremental consent.
                    * You must then, add the logic to delegate the consent screen to your client application here.
                    * This sample doesn't use the incremental consent strategy.
                    */
                    throw;
                }
                catch (Exception)
                {
                    throw;
                }
        }

        /// <summary>
        /// Public client builder app creating token using username and password
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="authority"></param>
        /// <returns>bearer token</returns>
        public static async Task<string> PublicClientBuilderApp(IEnumerable<string> scope, string tenantId, string clientId = null,
           string userName = null, string password = null)
        {
            ConvertStringToSecureString convertToSecureString = new ConvertStringToSecureString();
            ExponentialRetryStrategy retryStrategy = new ExponentialRetryStrategy();

            // We will use MSAL.NET to get a token to call the API On Behalf Of the current user
            try
            {
                _publicClientApp = PublicClientApplicationBuilder.Create(clientId).WithAuthority(AzureCloudInstance.AzurePublic, 
                    tenantId).WithLegacyCacheCompatibility(false).Build();

                // Acquiring an AuthenticationResult for the scope user.read, impersonating the user represented by userAssertion, using the OBO flow
                await RetryHelper.RetryAsync(async () =>
                {
                    //To clear the token cache every time the new request comes , else it uses cached userassertion
                    var accounts = await _publicClientApp.GetAccountsAsync().ConfigureAwait(false);

                    IEnumerator<IAccount> enumerator = accounts.GetEnumerator();
                    while (enumerator.Current != null)
                    {
                        await _publicClientApp.RemoveAsync(enumerator.Current).ConfigureAwait(false);

                    }
                    // Acquiring an AuthenticationResult for the scope user.read, impersonating the user represented by userAssertion, using the OBO flow
                    _authenticationResult = await _publicClientApp.AcquireTokenByUsernamePassword(scope, userName, convertToSecureString.StringToSecureString(password)).ExecuteAsync().ConfigureAwait(false);
                }, (exception) =>
                {
                    return true;
                }, retryStrategy.ShouldRetry(), new Action<Exception, RetryErrorType, int>(LogTokenGenerationFailure)).ConfigureAwait(false);

                if (_authenticationResult != null && !string.IsNullOrEmpty(_authenticationResult.AccessToken))
                    accessToken = _authenticationResult.AccessToken;

                if (accessToken == null)
                {
                    throw new Exception("Access Token could not be acquired.");
                }

                return accessToken;
            }
            catch (MsalUiRequiredException)
            {
                /*
                * If you used the scope `.default` on the client application, the user would have been prompted to consent for Graph API back there
                * and no incremental consents are required (this exception is not expected). However, if you are using the scope `access_as_user`,
                * this exception will be thrown at the first time the API tries to access Graph on behalf of the user for an incremental consent.
                * You must then, add the logic to delegate the consent screen to your client application here.
                * This sample doesn't use the incremental consent strategy.
                */
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static void LogTokenGenerationFailure(Exception exception, RetryErrorType errorType, int retryCount)
        {
        }
    }
}