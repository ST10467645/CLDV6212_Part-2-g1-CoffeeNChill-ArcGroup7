// One row in the Orders Azure Table.
// Attribution: adapted from the StudentQueryEntity example 
// ITableEntity usage per Microsoft Learn (2026c).
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