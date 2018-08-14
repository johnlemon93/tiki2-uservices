using System.Security.Claims;
using Nancy;
using Nancy.Owin;
using Nancy.TinyIoc;
using LibOwin;

namespace Uservice.Platform
{
    public static class UservicePlatform
    {
        private static string TokenUrl;
        private static string ClientName;
        private static string ClientSecret;

        public static void Configure(string tokenUrl, string clientName, string clientSecret)
        {
            TokenUrl = tokenUrl;
            ClientName = clientName;
            ClientSecret = clientSecret;
        }

        public static TinyIoCContainer UseHttpClientFactory(this TinyIoCContainer self, NancyContext context)
        {

            // reads the end user from the OWIn environment
            object key = null;
            context.GetOwinEnvironment()?.TryGetValue(OwinConstants.RequestUser, out key);
            // get the end user's identity token from the user object
            var principal = key as ClaimsPrincipal;
            var idToken = principal?.FindFirst("id_token");

            // reads the correlation token from OWIN environment
            var correlationToken = context.GetOwinEnvironment()?["correlationToken"] as string;

            // Registers the HttpClientFactory as a per - request dependency in Nancy’s container
            self.Register<IHttpClientFactory>(new HttpClientFactory(TokenUrl, ClientName, ClientSecret, correlationToken ?? "", idToken?.Value ?? ""));

            return self;
        }
    }
}
