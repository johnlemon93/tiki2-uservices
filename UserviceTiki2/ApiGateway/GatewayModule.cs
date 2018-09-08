using System.Collections.Generic;
using System.Net.Http;
using Uservice.Platform;
using Nancy;
using Serilog;

namespace ApiGateway
{
    public class GatewayModule : NancyModule
    {
        public GatewayModule(IHttpClient httpClient, ILogger logger)
        {
            Get("/productlist", async _ =>
            {
                var request = httpClient.CreateRequest("http://localhost:54080/products?productIds=1,2,3,4", HttpMethod.Get);
                var response = await httpClient.Client.SendAsync(request).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                logger.Information(content);

                return content;
            });
        }
    }   
}