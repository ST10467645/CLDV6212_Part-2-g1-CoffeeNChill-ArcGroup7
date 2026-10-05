// Attribution: entity model pattern adapted from the lecturer's course example (IIE, 2026)
// and the Azure Tables client library documentation (Microsoft Learn, 2026c).
using Azure;
using Azure.Data.Tables;

namespace CoffeeNChillFunctions.Models
{
    public class MenuItem : ITableEntity
    {
        public string PartitionKey { get; set; } = default!; // Category, e.g. "Hot Drinks"
        public string RowKey { get; set; } = default!;       // SKU, e.g. "COF-001"
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public double Price { get; set; }
        public bool IsAvailable { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}