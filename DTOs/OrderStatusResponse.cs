/*
 * Code Attribution:
 * The HTTP response DTO pattern was based on Microsoft's official Azure
 * Functions HTTP trigger documentation. The response structure for the
 * optional order status endpoint was based on the lecturer-provided
 * CLDV6212 course example.
 *
 * Microsoft Learn (2025b). Azure Functions HTTP trigger. [Online]
 * Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger?tabs=python-v2%2Cisolated-process%2Cnodejsv4%2Cfunctionsv2&pivots=programming-language-csharp
 * [Accessed 04 September 2026].
 *
 * CLDV6212 lecturer-provided optional order status endpoint example.
 */

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