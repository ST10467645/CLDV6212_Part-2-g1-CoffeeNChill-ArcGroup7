/*
 * Code Attribution:
 * The Azure Table Storage entity model pattern, including implementation
 * of ITableEntity and the use of PartitionKey, RowKey, Timestamp and ETag,
 * was adapted from the lecturer-provided course example and Microsoft's
 * official Azure Tables client library documentation.
 *
 * Lecturer CLDV6212 course example.
 *
 * Microsoft Learn (2026c). Azure Tables client library for .NET - version 12.12.0. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
 * [Accessed 04 September 2026].
 */

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