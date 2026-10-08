// Attribution:
// response DTO pattern based on the Azure Functions HTTP trigger documentation (Microsoft Learn, 2025b);
// the reply shape follows the optional status endpoint example given by the lecturer (IIE, 2026).
using System.Text.Json.Serialization;

namespace CoffeeNChillFunctions.DTOs
{
    // Defines the DTO that is returned when someone checks the status of an order, it only holds the order id and the status.
    public class OrderStatusResponse
    {
        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}