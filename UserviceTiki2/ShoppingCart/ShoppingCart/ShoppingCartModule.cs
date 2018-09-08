using Nancy;
using Nancy.ModelBinding;
using ShoppingCart.EventFeed;

namespace ShoppingCart.ShoppingCart
{
    public class ShoppingCartModule : NancyModule
    {
        public ShoppingCartModule(IShoppingCartStore shoppingCartStore, IProductCatalogClient productCatalog, IEventStore eventStore) 
            : base("/shoppingcart")
        {
            Get("/{userid:int}", parameters =>
            {
                var userId = (int)parameters.userud;
                return shoppingCartStore.Get(userId);
            });

            Post("/{userid:int}/items", async (parameters, _) =>
            {
                var productCatalogIds = this.Bind<int[]>();
                var userId = (int)parameters.userid;

                var shoppingCart = await shoppingCartStore.Get(userId).ConfigureAwait(false);
                var shoppingCartItems = await productCatalog.GetShoppingCartItems(productCatalogIds).ConfigureAwait(false);

                shoppingCart.AddItems(shoppingCartItems, eventStore);
                await shoppingCartStore.Save(shoppingCart);

                return shoppingCart;
            });

            Delete("/{userid:int}", async parameters =>
            {
                var productCatalogIds = this.Bind<int[]>();
                var userId = (int)parameters.userid;

                var shoppingCart = await shoppingCartStore.Get(userId).ConfigureAwait(false);
                shoppingCart.RemoveItems(productCatalogIds, eventStore);
                await shoppingCartStore.Save(shoppingCart);

                return shoppingCart;
            });
        }
    }
}
