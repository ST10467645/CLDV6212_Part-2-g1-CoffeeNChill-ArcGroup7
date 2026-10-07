/* Attribution:
 * separating the request model from the queue message model follows the lecturer's StudentQuery
 * examples; the fields are taken from the CLDV6212 POE brief.
 */

namespace CoffeeNChillFunctions.DTOs
{
    public class CreateOrderRequest
    {
        public string? OrderId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public List<string>? SelectedItemSKUs { get; set; }

        public double TotalPrice { get; set; }

        public DateTime? OrderTimestamp { get; set; }
    }
}