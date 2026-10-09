/*
 * Code Attribution:
 * The HTTP response DTO pattern was based on Microsoft's official Azure
 * Functions HTTP trigger documentation.
 *
 * Microsoft Learn (2025b). Azure Functions HTTP trigger. [Online]
 * Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger?tabs=python-v2%2Cisolated-process%2Cnodejsv4%2Cfunctionsv2&pivots=programming-language-csharp
 * [Accessed 04 September 2026].
 */

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