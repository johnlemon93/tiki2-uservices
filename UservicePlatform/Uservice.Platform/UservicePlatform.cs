using System.Security.Claims;
using Nancy;
using Nancy.Owin;
using Nancy.TinyIoc;
using LibOwin;

namespace Uservice.Platform
{
    public static class UservicePlatform
    {
        private static string TokenUrl = string.Empty;
        private static string ClientName = "DefaultId";
        private static string ClientSecret = string.Empty;

        public static void Configure(string tokenUrl, string clientName, string clientSecret)
        {
            TokenUrl = tokenUrl;
            ClientName = clientName;
            ClientSecret = clientSecret;
        }

        public static TinyIoCContainer UseHttpClient(this TinyIoCContainer self, NancyContext context)
        {
            // reads the end user from the OWIN environment
            object key = null;
            context.GetOwinEnvironment()?.TryGetValue(OwinConstants.RequestUser, out key);
            // get the end user's identity token from the user object
            var principal = key as ClaimsPrincipal;
            var idToken = principal?.FindFirst("id_token");

            // reads the correlation token from OWIN environment
            var correlationToken = context.GetOwinEnvironment()?["correlationToken"] as string;

            // Registers the UserviceHttpClient as a per-request dependency in Nancy’s container
            self.Register<IHttpClient>(new UserviceHttpClient(TokenUrl, ClientName, ClientSecret, correlationToken ?? "", idToken?.Value ?? ""));

            return self;
        }
    }
}
