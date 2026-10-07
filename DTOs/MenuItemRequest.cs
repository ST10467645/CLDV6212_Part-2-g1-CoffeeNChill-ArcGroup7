// Attribution:
// separating the request model from the table entity follows the lecturer's StudentQuery and
// StudentQueryEntity examples (IIE, 2026); entity handling per the Azure Tables client library documentation (Microsoft Learn, 2026c).
using System.Text.Json.Serialization;

namespace CoffeeNChillFunctions.DTOs
{
    // Defines the DTO (data transfer object) that holds the JSON sent when a new menu item is created.
    // It is kept separate from the MenuItem table entity, so that storage details like the ETag never become part of the request.
    public class MenuItemRequest
    {
        [JsonPropertyName("PartitionKey")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("RowKey")]
        public string Sku { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public double Price { get; set; }

        public bool IsAvailable { get; set; }
    }

    // Defines the DTO that holds the JSON sent when an existing menu item is updated.
    // Every property is optional, so only the values that are sent are changed.
    public class MenuItemUpdateRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public double? Price { get; set; }
        public bool? IsAvailable { get; set; }
    }
}