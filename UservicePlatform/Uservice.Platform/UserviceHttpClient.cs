using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IdentityModel.Client;

namespace Uservice.Platform
{
    public interface IHttpClient
    {
        /// <summary>
        /// A shared instance of <see cref="HttpClient"/> to avoid SocketException error
        /// </summary>
        HttpClient Client { get; }

        /// <summary>
        /// Returns an instance of <see cref="HttpRequestMessage"/> with correlation token, user id and access token in the headers
        /// </summary>
        Task<HttpRequestMessage> CreateRequest(string requestScope, string requestUri, HttpMethod method, 
            string mediaType = "application/json", HttpContent content = null);

        /// <summary>
        /// Returns an instance of <see cref="HttpRequestMessage"/> with correlation token and user id token in the headers 
        /// </summary>
        HttpRequestMessage CreateRequest(string requestUri, HttpMethod method, 
            string mediaType = "application/json", HttpContent content = null);
    }

    /// <summary>
    /// Use this to create and send Http Request to make sure all outgoing request includes correlation token 
    /// as well as authentication header and user identity token (if the request originates from an end user request)
    /// </summary>
    public class UserviceHttpClient : IHttpClient
    {
        private readonly TokenClient m_TokenClient;
        private readonly string m_CorrelationToken;
        private readonly string m_IdToken;

        /// <summary>
        /// A shared instance of <see cref="HttpClient"/> to avoid SocketException error
        /// <para>
        /// HttpClient is intended to be instantiated once and re-used throughout the life of an application. 
        /// Especially in server applications, creating a new HttpClient instance for every request will exhaust the number of sockets available under heavy loads. 
        /// This will result in SocketException errors.
        /// </para>
        /// https://stackoverflow.com/questions/40187153/httpclient-in-using-statement
        /// https://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
        /// https://medium.com/@nuno.caneco/c-httpclient-should-not-be-disposed-or-should-it-45d2a8f568bc
        /// https://blogs.msdn.microsoft.com/shacorn/2016/10/21/best-practices-for-using-httpclient-on-services/
        /// </summary>
        private static readonly HttpClient SharedClient = new HttpClient();
        public HttpClient Client => SharedClient;

        /// <summary>
        /// Initializes a new instance of UserviceHttpClient class.
        /// </summary>
        /// <param name="tokenUrl">URL of the 'OpenID Connect/OAuth 2.0' token endpoint in the Login microservice</param>
        /// <param name="clientId">along with <param name="clientSecret">clientSecret</param> used to obtain an access token from the token endpoint</param>
        /// <param name="correlationToken">per-request correlation token coming from a piece of middleware</param>
        /// <param name="idToken">Token with the end user's identity</param>
        public UserviceHttpClient(string tokenUrl, string clientId, string clientSecret, string correlationToken, string idToken)
        {
            m_TokenClient = new TokenClient(tokenUrl, clientId, clientSecret);
            m_CorrelationToken = correlationToken;
            m_IdToken = idToken;
        }

        public HttpRequestMessage CreateRequest(string requestUri, HttpMethod method, string mediaType = "application/json", HttpContent content = null)
        {
            var request = new HttpRequestMessage
            {
                Method = method,
                RequestUri = new Uri(requestUri)
            };

            request.Headers.Add("Accept", mediaType);

            if (content != null)
            {
                request.Content = content;
            }

            // adds the correlation token to a request header
            request.Headers.Add("Correlation-Token", m_CorrelationToken);

            // adds the end user's identity to a request header in case the request originates from an end user request
            if (!string.IsNullOrEmpty(m_IdToken))
            {
                request.Headers.Add("uservice-end-user", m_IdToken);
            }

            return request;
        }

        public async Task<HttpRequestMessage> CreateRequest(string requestScope, string requestUri, HttpMethod method, string mediaType = "application/json", HttpContent content = null)
        {
            var request = CreateRequest(requestUri, method, mediaType, content);

            // requests an authorization token from the Login microservice, allowing calls that require the scope in requestScope
            var response = await m_TokenClient.RequestClientCredentialsAsync(requestScope).ConfigureAwait(false);
            // adds the authorization token to a request header
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", response.AccessToken);

            return request;
        }
    }
}
