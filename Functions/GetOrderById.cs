// Returns one order using its order date (PartitionKey) and OrderId (RowKey); 404 if it does not exist.
// Attribution: existence-check pattern adapted from the lecturer's examples (IIE, 2026)
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
    // Defines the Azure Function responsible for retrieving a single order from the Orders table.
    public class GetOrderById
    {
        // The logger used to write information, warnings and errors to the console.
        private readonly ILogger _logger;

        // The Functions host provides the logger automatically (dependency injection), the same way as in the Part 1 functions.
        public GetOrderById(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetOrderById>();
        }

        // Exposes this method as an Azure Function that handles HTTP GET requests using the order date and order id supplied in the route.
        [Function("GetOrderById")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders/{orderDate}/{orderId}")] HttpRequestData req,
            string orderDate, string orderId)
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

                // Looks up the order using its partition key (order date) and row key (order id).
                try
                {
                    var found = await table.GetEntityAsync<OrderEntity>(orderDate, orderId);

                    // Returns 200 OK with the order as an OrderResponse DTO, so that the ETag and Timestamp are not returned to the client.
                    response.StatusCode = HttpStatusCode.OK;
                    await response.WriteAsJsonAsync(OrderResponse.From(found.Value));
                    return response;
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    // Returns a 404 Not Found response when the requested order does not exist, instead of an unhandled exception.
                    _logger.LogWarning("Order {OrderId} on {OrderDate} not found.", orderId, orderDate);
                    response.StatusCode = HttpStatusCode.NotFound;
                    await response.WriteAsJsonAsync(new ErrorResponse($"Order '{orderId}' for date '{orderDate}' not found."));
                    return response;
                }
            }
            catch (RequestFailedException ex)
            {
                // Handles errors returned by Azure Table Storage and returns a 500 Internal Server Error with the Azure error details.
                _logger.LogError(ex, "Azure Table error while reading an order.");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteAsJsonAsync(new ErrorResponse($"Azure Table Error: {ex.ErrorCode} - {ex.Message}"));
                return response;
            }
            catch (Exception ex)
            {
                // Handles unexpected errors and returns a 500 Internal Server Error so that the client receives an appropriate failure response.
                _logger.LogError(ex, "Unexpected error while reading an order.");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteAsJsonAsync(new ErrorResponse($"Error: {ex.Message}"));
                return response;
            }
        }
    }
}