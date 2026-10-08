// Attribution:
// response DTO pattern based on the Azure Functions HTTP trigger documentation (Microsoft Learn, 2025b).
using System.Text.Json.Serialization;

namespace CoffeeNChillFunctions.DTOs
{
    public class OrderQueuedResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("orderDate")]
        public string OrderDate { get; set; } = string.Empty;
    }
}