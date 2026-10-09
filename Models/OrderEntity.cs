/*
 * Code Attribution:
 * The order table entity model was adapted from the StudentQueryEntity
 * example provided in the lecturer's course materials. The ITableEntity
 * implementation follows Microsoft's official Azure Tables client
 * library documentation.
 *
 * CLDV6212 lecturer-provided StudentQueryEntity course example.
 *
 * Microsoft Learn (2026c). Azure Tables client library for .NET - version 12.12.0. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
 * [Accessed 04 September 2026].
 */

using Azure;
using Azure.Data.Tables;

namespace CoffeeNChillFunctions.Models
{
    public class OrderEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string SelectedItemSKUs { get; set; } = string.Empty;
        public double TotalPrice { get; set; }
        public string OrderTimestamp { get; set; } = string.Empty;    // ISO 8601 text
        public string Status { get; set; } = string.Empty;            // Received / Preparing / Ready / Collected
        public string LastUpdated { get; set; } = string.Empty;       // ISO 8601 text
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}