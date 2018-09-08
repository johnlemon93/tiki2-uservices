using System.Collections.Generic;

namespace ApiGateway.Models
{
    public class Product
    {
        public string ProductName;
        public int ProductId;
    }

    public class ProductDetails
    {

    }

    public class ShoppingCart
    {
        public IEnumerable<ShoppingCartItem> Items { get; set; }
    }

    public class ShoppingCartItem
    {
        public int ProductCatalogueId { get; set; }
        public string ProductName { get; set; }
    }
}
