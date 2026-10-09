/*
 * Code Attribution:
 * The order model structure was adapted from the StudentQuery model in
 * the lecturer-provided Student Relations course example. The order
 * fields were based on the CLDV6212 POE brief.
 *
 * CLDV6212 lecturer-provided Student Relations course example.
 *
 * Lecturer CLDV6212 POE brief.
 */

namespace CoffeeNChillFunctions.Models
{
    public class Order
    {
        // Unique order number
        public string OrderId { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        // SKUs of the menu items ordered
        public List<string> SelectedItemSKUs { get; set; } = new();

        public double TotalPrice { get; set; }

        // When the order was placed (UTC)
        public DateTime OrderTimestamp { get; set; }
    }
}