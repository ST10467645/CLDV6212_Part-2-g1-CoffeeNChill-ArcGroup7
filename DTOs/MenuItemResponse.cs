// Attribution:
// separating the response model from the table entity follows the lecturer's model examples (IIE, 2026).
namespace CoffeeNChillFunctions.DTOs
{
    // Defines the DTO that is returned to the client for a menu item.
    // It hides the ETag and Timestamp and uses clear names (Category and Sku) instead of the table keys.
    public class MenuItemResponse
    {
        public string Category { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
        public bool IsAvailable { get; set; }
    }
}