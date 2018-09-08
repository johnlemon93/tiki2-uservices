using System.Threading.Tasks;

namespace ShoppingCart.ShoppingCart
{
    public interface IShoppingCartStore
    {
        Task<IShoppingCart> Get(int userId);
        Task Save(IShoppingCart shoppingCart);
    }
}