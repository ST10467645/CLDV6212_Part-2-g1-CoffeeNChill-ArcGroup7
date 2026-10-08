// Lets someone check the status of an order with only its order id, for example GET /api/orders/ORD-2026-8801
// Attribution: Table Storage query pattern adapted from the lecturer's examples (IIE, 2026)
// and the Azure Tables client library documentation (Microsoft Learn, 2026c).
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using CoffeeNChillFunctions.Models;
using CoffeeNChillFunctions.DTOs;

namespace CoffeeNChillFunctions.Functions
{
    // Defines the Azure Function responsible for returning only the order id and the current status of an order.
    public class GetOrderStatus
    {
        // The logger used to write information, warnings and errors to the console.
        private readonly ILogger _logger;

        // The Functions host provides the logger automatically (dependency injection), the same way as in the Part 1 functions.
        public GetOrderStatus(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetOrderStatus>();
        }

        // Exposes this method as an Azure Function that handles HTTP GET requests using the order id supplied in the route.
        [Function("GetOrderStatus")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders/{orderId}")] HttpRequestData req,
            string orderId)
        {
            // Creates the HTTP response that will be returned to the client.
            var response = req.CreateResponse();
            try
            {
                // Creates a client for accessing the Orders table in Azure Table Storage.
                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";
                var table = new TableClient(connectionString, "Orders");

                // Ensures that the Orders table exists before the order is looked up.
                await table.CreateIfNotExistsAsync();

                // The order date (PartitionKey) is not known here, so the table is searched by the order id (RowKey) instead.
                OrderEntity? found = null;
                await foreach (var entity in table.QueryAsync<OrderEntity>(x => x.RowKey == orderId))
                {
                    found = entity;
                    break;
                }

                // Returns a 404 Not Found response when no order has this id, instead of an unhandled exception.
                if (found == null)
                {
                    _logger.LogWarning("Order {OrderId} not found.", orderId);
                    response.StatusCode = HttpStatusCode.NotFound;
                    await response.WriteAsJsonAsync(new ErrorResponse($"Order '{orderId}' not found."));
                    return response;
                }

                // Returns 200 OK with only the order id and the status, as an OrderStatusResponse DTO.
                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(new OrderStatusResponse { OrderId = found.RowKey, Status = found.Status });
                return response;
            }
            catch (RequestFailedException ex)
            {
                // Handles errors returned by Azure Table Storage and returns a 500 Internal Server Error with the Azure error details.
                _logger.LogError(ex, "Azure Table error while reading the order status.");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteAsJsonAsync(new ErrorResponse($"Azure Table Error: {ex.ErrorCode} - {ex.Message}"));
                return response;
            }
            catch (Exception ex)
            {
                // Handles unexpected errors and returns a 500 Internal Server Error so that the client receives an appropriate failure response.
                _logger.LogError(ex, "Unexpected error while reading the order status.");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteAsJsonAsync(new ErrorResponse($"Error: {ex.Message}"));
                return response;
            }
        }
    }
}