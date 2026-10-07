// Attribution:
// mapper pattern for converting between request, queue message and response objects, kept in one place
// so the functions stay short (IIE, 2026).
using CoffeeNChillFunctions.Models;

namespace CoffeeNChillFunctions.DTOs
{
    // Defines the mapper that converts between the order DTOs and the Order queue message.
    public static class OrderMapper
    {
        // Converts a validated CreateOrderRequest into the Order that is placed on the queue.
        // The order number and the timestamp are generated here when the client did not send them.
        public static Order ToOrder(CreateOrderRequest dto) => new Order
        {
            OrderId = string.IsNullOrWhiteSpace(dto.OrderId) ? GenerateOrderId() : dto.OrderId.Trim(),
            CustomerName = dto.CustomerName.Trim(),
            SelectedItemSKUs = dto.SelectedItemSKUs!.Select(sku => sku.Trim()).ToList(),
            TotalPrice = dto.TotalPrice,
            OrderTimestamp = (dto.OrderTimestamp is null || dto.OrderTimestamp.Value == default)
                ? DateTime.UtcNow
                : dto.OrderTimestamp.Value
        };

        // Generates an order number that looks like ORD-2026-A1B2C3.
        public static string GenerateOrderId() =>
            $"ORD-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

        // Converts the accepted Order into the response that is returned to the client.
        public static OrderQueuedResponse ToQueuedResponse(Order order) => new OrderQueuedResponse
        {
            Message = "Order accepted and queued for processing.",
            OrderId = order.OrderId,
            OrderDate = order.OrderTimestamp.ToString("yyyy-MM-dd")
        };
    }
}