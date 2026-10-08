// Returns all orders from the Orders table, optionally filtered by status (?status=Received).
// Attribution: Table Storage query pattern adapted from the lecturer's examples (IIE, 2026)
// and the Azure Tables client library documentation (Microsoft Learn, 2026c).
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Web;
using CoffeeNChillFunctions.Models;
using CoffeeNChillFunctions.DTOs;

namespace CoffeeNChillFunctions.Functions
{
    // Defines the Azure Function responsible for retrieving all orders, or only the orders with a specific status.
    public class GetOrders
    {
        // The logger used to write information, warnings and errors to the console.
        private readonly ILogger _logger;

        // The Functions host provides the logger automatically (dependency injection), the same way as in the Part 1 functions.
        public GetOrders(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetOrders>();
        }

        // Exposes this method as an Azure Function that can be accessed through an HTTP GET request at the /orders route.
        [Function("GetOrders")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders")] HttpRequestData req)
        {
            // Creates the HTTP response that will be returned to the client.
            var response = req.CreateResponse();
            try
            {
                // Reads the optional status filter from the query string, for example /orders?status=Received.
                string? status = HttpUtility.ParseQueryString(req.Url.Query)["status"];

                // Returns 400 Bad Request when a status was supplied that is not one of the four allowed statuses.
                if (!string.IsNullOrWhiteSpace(status) && !OrderStatus.IsValid(status))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteAsJsonAsync(new ErrorResponse("Status must be one of: Received, Preparing, Ready, Collected."));
                    return response;
                }

                // Creates a client for accessing the Orders table in Azure Table Storage.
                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";
                var table = new TableClient(connectionString, "Orders");

                // Ensures that the Orders table exists, so that an empty system returns an empty list instead of an error.
                await table.CreateIfNotExistsAsync();

                // Filters on the server side when a status was supplied, otherwise all the orders are queried.
                var results = string.IsNullOrWhiteSpace(status)
                    ? table.QueryAsync<OrderEntity>()
                    : table.QueryAsync<OrderEntity>(x => x.Status == status);

                // Stores the orders as OrderResponse DTOs, so that the ETag and Timestamp are not returned to the client.
                var orders = new List<OrderResponse>();
                await foreach (var entity in results)
                {
                    orders.Add(OrderResponse.From(entity));
                }

                _logger.LogInformation("Returned {Count} order(s).", orders.Count);

                // Returns 200 OK with the list of orders. If no orders match, an empty array is returned rather than an error.
                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(orders);
                return response;
            }
            catch (RequestFailedException ex)
            {
                // Handles errors returned by Azure Table Storage and returns a 500 Internal Server Error with the Azure error details.
                _logger.LogError(ex, "Azure Table error while reading orders.");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteAsJsonAsync(new ErrorResponse($"Azure Table Error: {ex.ErrorCode} - {ex.Message}"));
                return response;
            }
            catch (Exception ex)
            {
                // Handles unexpected errors and returns a 500 Internal Server Error so that the client receives an appropriate failure response.
                _logger.LogError(ex, "Unexpected error while reading orders.");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteAsJsonAsync(new ErrorResponse($"Error: {ex.Message}"));
                return response;
            }
        }
    }
}