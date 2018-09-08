using Dapper;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace ShoppingCart.ShoppingCart
{
    public class ShoppingCartStore : IShoppingCartStore
    {
        private const string ConnectionString = "Data Source=localhost;Initial Catalog=ShoppingCart;Integrated Security=True";

        public async Task<IShoppingCart> Get(int userId)
        {
            const string readItemsSql = @"SELECT * FROM ShoppingCart, ShoppingCartItems 
                                          WHERE ShoppingCartItems.ShoppingCartId = ID 
                                            AND ShoppingCart.UserId=@UserId";
            using (var conn = new SqlConnection(ConnectionString))
            {
                var items = await conn.QueryAsync<ShoppingCartItem>(readItemsSql, new { UserId = userId });
                return new ShoppingCart(userId, items);
            }
        }

        public async Task Save(IShoppingCart shoppingCart)
        {
            const string deleteAllForShoppingCartSql = @"DELETE item FROM ShoppingCartItems item
                                                         INNER JOIN ShoppingCart cart ON item.ShoppingCartId = cart.ID
                                                                                      AND cart.UserId=@UserId";

            const string addAllForShoppingCartSql = @"INSERT INTO ShoppingCartItems 
                                                     (ShoppingCartId, ProductCatalogId, ProductName, ProductDescription, Amount, Currency)
                                                     VALUES (@ShoppingCartId, @ProductCatalogId, @ProductName,v@ProductDescription, @Amount, @Currency)";

            using (var conn = new SqlConnection(ConnectionString))
            using (var tx = conn.BeginTransaction())
            {
                // deletes all preexisting shopping cart items
                await conn.ExecuteAsync(deleteAllForShoppingCartSql, new { shoppingCart.UserId }, tx).ConfigureAwait(false);

                // adds the current shopping cart items
                await conn.ExecuteAsync(addAllForShoppingCartSql, shoppingCart.Items, tx).ConfigureAwait(false);
            }
        }

    }
}
