/*
 * Code Attribution:
 * Separating the menu item request model from the table entity was based on
 * the lecturer-provided StudentQuery and StudentQueryEntity course examples.
 * Azure Table Storage entity handling follows Microsoft's official Azure
 * Tables client library documentation.
 *
 * CLDV6212 lecturer-provided StudentQuery and StudentQueryEntity course examples.
 *
 * Microsoft Learn (2026c). Azure Tables client library for .NET - version 12.12.0. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
 * [Accessed 04 September 2026].
 */

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