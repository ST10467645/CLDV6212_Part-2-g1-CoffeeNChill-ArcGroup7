/*
 * DTO (data transfer object) returned to clients so table internals (ETag,
 * Timestamp) stay hidden and the comma-joined SKU text is turned back into a list.
 *
 * Code Attribution:
 * The entity-to-response mapping approach was based on Microsoft's official
 * Azure Tables client library documentation.
 *
 * Microsoft Learn (2026c). Azure Tables client library for .NET - version 12.12.0. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
 * [Accessed 04 September 2026].
 */

using CoffeeNChillFunctions.Models;

namespace CoffeeNChillFunctions.DTOs
{
    public class OrderResponse
    {
        public string OrderDate { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public List<string> SelectedItemSKUs { get; set; } = new();
        public double TotalPrice { get; set; }
        public string OrderTimestamp { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string LastUpdated { get; set; } = string.Empty;

        // Converting the stored table row into the response shape.
        public static OrderResponse From(OrderEntity e) => new()
        {
            OrderDate = e.PartitionKey,
            OrderId = e.RowKey,
            CustomerName = e.CustomerName,
            SelectedItemSKUs = string.IsNullOrWhiteSpace(e.SelectedItemSKUs)
                ? new List<string>()
                : e.SelectedItemSKUs.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            TotalPrice = e.TotalPrice,
            OrderTimestamp = e.OrderTimestamp,
            Status = e.Status,
            LastUpdated = e.LastUpdated
        };
    }
}