// Returns one order using its order date (PartitionKey) and OrderId (RowKey); 404 if it does not exist.
// Attribution:
// existence-check pattern adapted from the lecturer's examples (IIE, 2026)
// and the Azure Tables client library documentation (Microsoft Learn, 2026c).
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using CoffeeNChillFunctions.Models;

namespace CoffeeNChillFunctions.Functions
{
    public class GetOrderById
    {
        private readonly ILogger _logger;

        public GetOrderById(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetOrderById>();
        }

        // HTTP GET endpoint at /api/orders/{orderDate}/{orderId}.
        [Function("GetOrderById")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders/{orderDate}/{orderId}")] HttpRequestData req,
            string orderDate, string orderId)
        {
            var response = req.CreateResponse();
            try
            {
                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";
                var table = new TableClient(connectionString, "Orders");
                await table.CreateIfNotExistsAsync();

                // Returning a proper 404 when the order does not exist instead of an unhandled exception.
                try
                {
                    var found = await table.GetEntityAsync<OrderEntity>(orderDate, orderId);
                    response.StatusCode = HttpStatusCode.OK;
                    await response.WriteAsJsonAsync(OrderResponse.From(found.Value));
                    return response;
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    _logger.LogWarning("Order {OrderId} on {OrderDate} not found.", orderId, orderDate);
                    response.StatusCode = HttpStatusCode.NotFound;
                    await response.WriteAsJsonAsync(new { error = $"Order '{orderId}' for date '{orderDate}' not found." });
                    return response;
                }
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Table error while reading an order.");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync($"Azure Table Error: {ex.ErrorCode} - {ex.Message}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while reading an order.");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync($"Error: {ex.Message}");
                return response;
            }
        }
    }
}