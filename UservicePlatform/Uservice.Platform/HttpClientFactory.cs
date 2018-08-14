using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IdentityModel.Client;

namespace Uservice.Platform
{
    public interface IHttpClientFactory
    {
        Task<HttpClient> Create(Uri uri, string requestScope);
    }

    /// <summary>
    /// Use this to create HttpClient to make sure all outgoing request includes correlation token 
    /// as well as authentication header and user identity token (if the request originates from an end user request)
    /// </summary>
    public class HttpClientFactory : IHttpClientFactory
    {
        private readonly TokenClient m_TokenClient;
        private readonly string m_CorrelationToken;
        private readonly string m_IdToken;

        /// <summary>
        /// Initializes a new instance of HttpClientFactory class.
        /// </summary>
        /// <param name="tokenUrl">URL of the 'OpenID Connect/OAuth 2.0' token endpoint in the Login microservice</param>
        /// <param name="clientName">along with <param name="clientSecret">clientSecret</param> used to obtain an access token from the token endpoint</param>
        /// <param name="correlationToken">per-request correlation token coming from a piece of middleware</param>
        /// <param name="idToken">Token with the end user's identity</param>
        public HttpClientFactory(string tokenUrl, string clientName, string clientSecret, string correlationToken, string idToken)
        {
            m_TokenClient = new TokenClient(tokenUrl, clientName, clientSecret);
            m_CorrelationToken = correlationToken;
            m_IdToken = idToken;
        }

        public async Task<HttpClient> Create(Uri uri, string requestScope)
        {
            // requests an authorization token from the Login microservice, allowing calls that require the scope in requestScope
            var response = await m_TokenClient.RequestClientCredentialsAsync(requestScope).ConfigureAwait(false);

            // prepares the client to make requests to URI
            var client = new HttpClient() { BaseAddress = uri };
            // adds the authorization token to a request header
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", response.AccessToken);
            // adds the correlation token to a request header
            client.DefaultRequestHeaders.Add("Correlation-Token", m_CorrelationToken);

            // adds the end user's identity to a request header in case the request originates from an end user request
            if (!string.IsNullOrEmpty(m_IdToken))
            {
                client.DefaultRequestHeaders.Add("uservice-end-user", m_IdToken);
            }

            return client;
        }
    }
}
