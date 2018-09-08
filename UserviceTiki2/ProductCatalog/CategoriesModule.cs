using Nancy;

namespace ProductCatalog
{
    public class CategoriesModule : NancyModule
    {
        public CategoriesModule(IProductCatalogStore catalogStore) : base("/categories")
        {
            Get("", async _ =>
            {
                var categories = await catalogStore.GetCategories();

                return
                   Negotiate
                   .WithModel(categories)
                   .WithHeader("cache-control", "max-age:86400");
            });
        }
    }
}
