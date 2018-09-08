using Nancy;
using System.Collections.Generic;
using System.Linq;

namespace ProductCatalog
{
    public class ProductsModule : NancyModule
    {
        public ProductsModule(IProductCatalogStore catalogStore) : base("/products")
        {
            Get("", async _ =>
            {
                string productIdsString = Request.Query.productIds;
                IEnumerable<Product> products;

                if (!string.IsNullOrEmpty(productIdsString))
                {
                    var productIds = ParseProductIdsFromQueryString(productIdsString);
                    products = await catalogStore.GetProductsByIds(productIds);
                }
                else if(int.TryParse(Request.Query.categoryId, out int categoryId))
                {
                    products = await catalogStore.GetProductsByCategory(categoryId);
                }
                else
                {
                    products = await catalogStore.GetProducts();
                }

                return
                   Negotiate
                   .WithModel(products)
                   .WithHeader("cache-control", "max-age:86400");
            });
        }

        private static IEnumerable<int> ParseProductIdsFromQueryString(string productIdsString)
        {
            return productIdsString.Split(',').Select(s => s.Replace("[", "").Replace("]", "")).Select(int.Parse);
        }
    }
}
