using System.Collections.Generic;
using System.Linq;

namespace ProductCatalog
{
    public class Entity
    {
        public int Id { get; set; }
        public Dictionary<string, dynamic> Attributes { get; } = new Dictionary<string, dynamic>();

        public Entity(IGrouping<dynamic, dynamic> dbData)
        {
            Id = dbData.Key;

            foreach (var attribute in dbData)
            {
                Attributes[attribute.AttributeCode] = attribute.Value;
            }
        }
    }

    public class Category : Entity
    {
        public int ParentId { get; set; }
        public string Path { get; set; }
        public int Position { get; set; }
        public int Level { get; set; }

        public Category(IGrouping<dynamic, dynamic> dbData) : base(dbData)
        {
            var firstItem = dbData.First();
            ParentId = firstItem.ParentID;
            Path = firstItem.Path;
            Position = firstItem.Position;
            Level = firstItem.Level;
        }
    }

    public class Product : Entity
    {
        public string Sku { get; set; }

        public Product(IGrouping<dynamic, dynamic> dbData) : base(dbData)
        {
            var firstItem = dbData.First();
            Sku = firstItem.Sku;
        }
    }
}
