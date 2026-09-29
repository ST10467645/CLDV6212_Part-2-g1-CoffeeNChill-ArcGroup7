// Code attribution: entity model pattern adapted from lecturer provided course example and Microsoft's official
// Azure.Data.Tables documentation:
// Microsoft, "Azure Tables client library for .NET," Microsoft Learn,6 May 2025.
// https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
// [Accessed: 26-Aug-2026].
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