using ShoppingCart.ShoppingCart;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoppingCart
{
    public interface IProductCatalogClient
    {
        Task<IEnumerable<ShoppingCartItem>> GetShoppingCartItems(int[] productCatalogIds);
    }
}