using ShoppingCart.EventFeed;
using System.Collections.Generic;
using System.Linq;

namespace ShoppingCart.ShoppingCart
{
    public class ShoppingCart : IShoppingCart
    {
        public int UserId { get; }

        private HashSet<ShoppingCartItem> m_Items = new HashSet<ShoppingCartItem>();
        public IEnumerable<ShoppingCartItem> Items { get { return m_Items; } }

        public ShoppingCart(int userId)
        {
            UserId = userId;
        }

        public ShoppingCart(int userId, IEnumerable<ShoppingCartItem> items)
        {
            UserId = userId;
            foreach (var item in items)
            {
                m_Items.Add(item);
            }
        }

        public void AddItems(IEnumerable<ShoppingCartItem> shoppingCartItems, IEventStore eventStore)
        {
            foreach(var item in shoppingCartItems)
            {
                if (m_Items.Add(item))
                {
                    eventStore.Raise("ShoppingCartItemAdded", new { UserId, item });
                }
            }
        }

        public void RemoveItems(int[] productCatalogIds, IEventStore eventStore)
        {
            var itemsToRemove = m_Items.Where(item => productCatalogIds.Contains(item.ProductCatalogId));
            foreach (var item in itemsToRemove)
            {
                if (m_Items.Remove(item))
                {
                    eventStore.Raise("ShoppingCartItemRemoved", new { UserId, item });
                }
            }
        }
    }
}
