/*
 * Code Attribution:
 * Separating the request model from the queue message model was based on
 * the lecturer-provided StudentQuery examples. The request fields were
 * taken from the CLDV6212 POE brief.
 *
 * CLDV6212 lecturer-provided StudentQuery course example.
 *
 * Lecturer CLDV6212 POE brief.
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