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

namespace CoffeeNChillFunctions.Functions
{
    public class GetOrders
    {
        private readonly ILogger _logger;

        public GetOrders(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetOrders>();
        }

        // HTTP GET endpoint at /api/orders.
        [Function("GetOrders")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders")] HttpRequestData req)
        {
            var response = req.CreateResponse();
            try
            {
                // Filter: /api/orders?status=Received
                string? status = HttpUtility.ParseQueryString(req.Url.Query)["status"];

                if (!string.IsNullOrWhiteSpace(status) && !OrderStatus.IsValid(status))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteAsJsonAsync(new
                    {
                        error = "Status must be one of: Received, Preparing, Ready, Collected."
                    });
                    return response;
                }

                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";
                var table = new TableClient(connectionString, "Orders");

                // Makeing sure the table exists so an empty system returns [] instead of an error.
                await table.CreateIfNotExistsAsync();

                // Filteing on the server side when a status was supplied.
                var results = string.IsNullOrWhiteSpace(status)
                    ? table.QueryAsync<OrderEntity>()
                    : table.QueryAsync<OrderEntity>(x => x.Status == status);

                var orders = new List<OrderResponse>();
                await foreach (var entity in results)
                {
                    orders.Add(OrderResponse.From(entity));
                }

                _logger.LogInformation("Returned {Count} order(s).", orders.Count);
                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(orders);
                return response;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Table error while reading orders.");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync($"Azure Table Error: {ex.ErrorCode} - {ex.Message}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while reading orders.");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync($"Error: {ex.Message}");
                return response;
            }
        }
    }
}