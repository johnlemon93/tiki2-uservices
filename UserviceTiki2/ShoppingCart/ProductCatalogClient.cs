using Newtonsoft.Json;
using Polly;
using ShoppingCart.ShoppingCart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Uservice.Platform;

namespace ShoppingCart
{
    public class ProductCatalogClient : IProductCatalogClient
    {
        private const string ProductCatalogBaseUrl = @"";
        private const string GetProductPathTemplate = "/products?productIds=[{0}]";

        private readonly ICache m_Cache;
        private readonly IHttpClient m_HttpClient;

        public ProductCatalogClient(ICache cache, IHttpClient httpClient)
        {
            m_Cache = cache;
            m_HttpClient = httpClient;
        }

        private async Task<HttpResponseMessage> RequestProductFromProductCatalog(int[] productCatalogIds)
        {
            var productsResource = string.Format(GetProductPathTemplate, string.Join(",", productCatalogIds));

            if (m_Cache.Get(productsResource) is HttpResponseMessage response)
            {
                return response;
            }

            var request = m_HttpClient.CreateRequest(ProductCatalogBaseUrl + productsResource, HttpMethod.Get);
            response = await m_HttpClient.Client.SendAsync(request).ConfigureAwait(false);
            AddToCache(productsResource, response);

            return response;
        }

        private void AddToCache(string resource, HttpResponseMessage response)
        {
            var cacheHeader = response.Headers.FirstOrDefault(h => h.Key == "cache-control");
            if (string.IsNullOrEmpty(cacheHeader.Key))
            {
                return;
            }

            var maxAge = CacheControlHeaderValue.Parse(cacheHeader.Value.ToString()).MaxAge;
            if (maxAge.HasValue)
            {
                m_Cache.Add(resource, response, maxAge.Value);
            }
        }

        private static async Task<IEnumerable<ShoppingCartItem>> ConvertToShoppingCartItems(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();
            var products = JsonConvert.DeserializeObject<List<ProductCatalogProduct>>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));

            return products
                    .Select(p => new ShoppingCartItem(
                        int.Parse(p.ProductId),
                        p.ProductName,
                        p.ProductDescription,
                        p.Price
                    ));
        }

        private async Task<IEnumerable<ShoppingCartItem>> GetItemsFromCatalogService(int[] productCatalogIds)
        {
            var response = await RequestProductFromProductCatalog(productCatalogIds)
                .ConfigureAwait(false);

            return await ConvertToShoppingCartItems(response)
                .ConfigureAwait(false);
        }

        private static Policy m_ExponentialRetryPolicy =
            Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)));


        public Task<IEnumerable<ShoppingCartItem>> GetShoppingCartItems(int[] productCatalogIds) =>
            m_ExponentialRetryPolicy.ExecuteAsync(async () => await GetItemsFromCatalogService(productCatalogIds).ConfigureAwait(false));


        private class ProductCatalogProduct
        {
            public string ProductId { get; set; }
            public string ProductName { get; set; }
            public string ProductDescription { get; set; }
            public Money Price { get; set; }
        }
    }
}
