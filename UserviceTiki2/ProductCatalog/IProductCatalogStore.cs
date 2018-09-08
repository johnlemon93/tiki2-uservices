using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductCatalog
{
    public interface IProductCatalogStore
    {
        Task<IEnumerable<Category>> GetCategories();

        Task<IEnumerable<Product>> GetProducts();
        Task<IEnumerable<Product>> GetProductsByCategory(int categoryId, bool includeChildren = false);
        Task<IEnumerable<Product>> GetProductsByIds(IEnumerable<int> productIds);
    }
}