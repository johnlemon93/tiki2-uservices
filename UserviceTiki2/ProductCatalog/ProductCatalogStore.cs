using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductCatalog
{
    public class ProductCatalogStore : IProductCatalogStore
    {
        private const string ConnectionString = "Data Source=localhost;Initial Catalog=ProductCatalog;Integrated Security=True";

        public async Task<IEnumerable<Category>> GetCategories()
        {
            var readItemsSql = QueryStringBuilder(
                "Category",
                new[] { "ID", "ParentID", "Path", "Position", "Level" },
                new[] { "name" });

            var items = await QueryAsync(readItemsSql.ToString());
            return items.GroupBy(i => i.ID).Select(g => new Category(g));
        }

        public async Task<IEnumerable<Product>> GetProducts()
        {
            //var readItemsSql = QueryStringBuilder(
            //    "Product",
            //    new[] { "ID", "Sku" },
            //    new[] { "name", "price", "image_url" });

            //var items = await QueryAsync(readItemsSql.ToString());
            //return items.GroupBy(i => i.ID).Select(g => new Product(g));

            var queryColumns = CreateSpStructuredParam<string>(new[] { "ID", "Sku" });
            var queryAttributes = CreateSpStructuredParam<string>(new[] { "name", "price", "image_url" });

            var items = await QueryAsync("QueryEntity",
                    new { EntityName = "Product", EntityType = 2, Columns = queryColumns, Attributes = queryAttributes },
                    CommandType.StoredProcedure);

            return items.GroupBy(i => i.ID).Select(g => new Product(g));
        }

        public async Task<IEnumerable<Product>> GetProductsByCategory(int categoryId, bool includeChildren = false)
        {
            //var readItemsSql = QueryStringBuilder(
            //    "Product",
            //    new[] { "ID", "Sku" },
            //    new[] { "name", "price", "image_url" });

            //var categoryIdConditionStr = includeChildren ?
            //    $@"WHERE CategoryID IN (SELECT ID FROM Category WHERE Path like '%{categoryId}%')" :
            //    $@"WHERE CategoryID={categoryId}";

            //readItemsSql.Append($@"AND e.ID IN (SELECT ProductID FROM CategoryProduct {categoryIdConditionStr})");

            //var items = await QueryAsync(readItemsSql.ToString());
            //return items.GroupBy(i => i.ID).Select(g => new Product(g));

            var queryColumns = CreateSpStructuredParam<string>(new[] { "ID", "Sku" });
            var queryAttributes = CreateSpStructuredParam<string>(new[] { "name", "price", "image_url" });

            var items = await QueryAsync("QueryEntity",
                    new
                    {
                        EntityName = "Product",
                        EntityType = 2,
                        Columns = queryColumns,
                        Attributes = queryAttributes,
                        CategoryId = categoryId,
                        IncludeChildren = includeChildren
                    },
                    CommandType.StoredProcedure);

            return items.GroupBy(i => i.ID).Select(g => new Product(g));
        }

        public async Task<IEnumerable<Product>> GetProductsByIds(IEnumerable<int> productIds)
        {
            var readItemsSql = QueryStringBuilder(
                "Product",
                new[] { "ID", "Sku" },
                new[] { "name", "description", "price", "image_url", "published_date" });

            var idsQueryStr = string.Join(",", productIds);
            readItemsSql.Append($@"AND e.ID IN ({idsQueryStr})");


            var items = await QueryAsync(readItemsSql.ToString());
            return items.GroupBy(i => i.ID).Select(g => new Product(g));
        }

        private async Task<IEnumerable<dynamic>> QueryAsync(string sql, object param = null, CommandType? commandType = null)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                return await conn.QueryAsync(sql, param, commandType: commandType);
            }
        }

        private static StringBuilder QueryStringBuilder(string entityTable, string[] columns, string[] attributes)
        {
            var builder = new StringBuilder();

            // select
            var columnsQueryStr = string.Join(",", columns.Select(c => $"e.{c}"));
            builder.Append($@"SELECT {columnsQueryStr}, a.Code as AttributeCode,");
            builder.Append(@"CASE a.DataType
                                WHEN 'varchar' THEN eVarchar.Value
                                WHEN 'int' THEN CAST(eInt.Value as VARCHAR(15))
                                WHEN 'text' THEN eText.Value
                                WHEN 'decimal' THEN CAST(eDecimal.Value as VARCHAR(20))
                                WHEN 'datetime' THEN CAST(eDatetime.Value as VARCHAR(50))
                                ELSE a.DataType
                             END AS Value");
            builder.Append("\n");

            // from
            var eType = entityTable == "Product" ? 2 : 1;
            builder.Append($@"FROM {entityTable} e 
                              LEFT JOIN Attribute a ON a.EntityType={eType}
                              LEFT JOIN {entityTable}VarcharAttributeValue eVarchar 
                                  ON eVarchar.{entityTable}ID = e.ID AND eVarchar.AttributeID = a.ID
                              LEFT JOIN {entityTable}TextAttributeValue eText 
                                  ON eText.{entityTable}ID = e.ID AND eText.AttributeID = a.ID
                              LEFT JOIN {entityTable}DecimalAttributeValue eDecimal 
                                  ON eDecimal.{entityTable}ID = e.ID AND eDecimal.AttributeID = a.ID
                              LEFT JOIN {entityTable}IntAttributeValue eInt 
                                  ON eInt.{entityTable}ID = e.ID AND eInt.AttributeID = a.ID
                              LEFT JOIN {entityTable}DatetimeAttributeValue eDatetime 
                                  ON eDatetime.{entityTable}ID = e.ID AND eDatetime.AttributeID = a.ID");
            builder.Append("\n");


            // where
            var attributesQueryStr = string.Join(",", attributes.Select(a => $"'{a}'"));
            builder.Append($@"WHERE a.Code IN ({attributesQueryStr})");
            builder.Append("\n");

            return builder;
        }

        private static DataTable CreateSpStructuredParam<T>(params object[] items)
        {
            var table = new DataTable();
            table.Columns.Add("Item", typeof(T));

            foreach (var item in items)
            {
                table.Rows.Add(item);
            }

            return table;
        }
    }
}
