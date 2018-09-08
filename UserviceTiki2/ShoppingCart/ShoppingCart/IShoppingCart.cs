using ShoppingCart.EventFeed;
using System.Collections.Generic;

namespace ShoppingCart.ShoppingCart
{
    public interface IShoppingCart
    {
        int UserId { get; }
        IEnumerable<ShoppingCartItem> Items { get; }

        void AddItems(IEnumerable<ShoppingCartItem> shoppingCartItems, IEventStore eventStore);
        void RemoveItems(int[] productCatalogIds, IEventStore eventStore);
    }
}
