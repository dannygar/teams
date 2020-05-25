using System;
using Microsoft.Identity.Client;

namespace Microsoft_Teams_Graph_RESTAPIs_Connect
{
    public static class MsalAppBuilder
    {
        public static IConfidentialClientApplication BuildConfidentialClientApplication()
        {
            IConfidentialClientApplication clientapp = ConfidentialClientApplicationBuilder.Create(Globals.ClientId)
                .WithTenantId(Globals.TenantId)
                .WithClientSecret(Globals.ClientSecret)
                .WithRedirectUri(Globals.RedirectUri)
                .WithAuthority(new Uri(string.Format(Globals.Authority, Globals.TenantId)))
                .Build();

            MSALPerUserMemoryTokenCache userTokenCache = new MSALPerUserMemoryTokenCache(clientapp.UserTokenCache);
            return clientapp;
        }

        public static void ClearUserTokenCache()
        {
            IConfidentialClientApplication clientapp = ConfidentialClientApplicationBuilder.Create(Globals.ClientId)
                  .WithClientSecret(Globals.ClientSecret)
                  .WithRedirectUri(Globals.RedirectUri)
                  .WithAuthority(new Uri(string.Format(Globals.Authority, Globals.TenantId)))
                  .Build();

            // We only clear the user's tokens.
            MSALPerUserMemoryTokenCache userTokenCache = new MSALPerUserMemoryTokenCache(clientapp.UserTokenCache);
            userTokenCache.Clear();
        }
    }
}