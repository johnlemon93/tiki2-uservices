namespace ShoppingCart.ShoppingCart
{
    public class ShoppingCartItem
    {
        public int Id { get; }
        public int ProductCatalogId { get; }
        public string ProductName { get; }
        public string ProductDescription { get; }
        public Money Price { get; }

        public ShoppingCartItem(int productCatalogId, string productName, string productDescription, Money price)
        {
            ProductCatalogId = productCatalogId;
            ProductName = productName;
            ProductDescription = productDescription;
            Price = price;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var that = obj as ShoppingCartItem;
            return Id.Equals(that.Id);
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

    }

    public class Money
    {
        public string Currency { get; }
        public decimal Amount { get; }

        public Money(string currency, decimal amount)
        {
            Currency = currency;
            Amount = amount;
        }
    }
}
