// The JSON the customer sends and the queue carries.
// Attribution: structure adapted from the StudentQuery model in the lecturer's Student Relations
// example fields taken from the CLDV6212 POE brief
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