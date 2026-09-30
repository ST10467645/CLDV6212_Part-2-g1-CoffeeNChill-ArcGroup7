// Queue PRODUCER: validates an order, turns it into JSON and places it on order-processing-queue.
// The customer gets an instant 202 Accepted; the order is processed later by ProcessOrderQueue.
//
// Attribution:
// - HTTP trigger and queue producer pattern adapted from the lecturer's SubmitStudentQuery example
//   and the HTTP trigger documentation (Microsoft Learn, 2025b).
// - QueueClient and Base64 message encoding per Microsoft Learn (2026h).
// - Trigger-side queue rules (message encoding) per Microsoft Learn (2026g).
using Azure;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using CoffeeNChillFunctions.Models;

namespace CoffeeNChillFunctions.Functions
{
    // Defines the Azure Function that accepts a new order and places it on the order queue.
    public class SubmitOrder
    {
        // The queue name is kept in one place so it always matches the trigger in ProcessOrderQueue.
        private const string QueueName = "order-processing-queue";

        private readonly ILogger _logger;

        // The Functions host provides the logger.
        public SubmitOrder(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SubmitOrder>();
        }

        // HTTP POST endpoint at /api/orders/queue.
        [Function("SubmitOrder")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders/queue")] HttpRequestData req)
        {
            try
            {
                // Reads the request body as text.
                string body = await new StreamReader(req.Body).ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(body))
                {
                    return await WriteJson(req, HttpStatusCode.BadRequest,
                        new { errors = new[] { "Request body is empty." } });
                }

                // Converts the JSON text into an Order object.
                Order? order;
                try
                {
                    order = JsonSerializer.Deserialize<Order>(body,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException)
                {
                    return await WriteJson(req, HttpStatusCode.BadRequest,
                        new { errors = new[] { "Request body is not valid JSON." } });
                }

                if (order == null)
                {
                    return await WriteJson(req, HttpStatusCode.BadRequest,
                        new { errors = new[] { "Order could not be read." } });
                }

                // Validates every field and collects ALL problems so the client can fix them in one go.
                var errors = new List<string>();

                if (string.IsNullOrWhiteSpace(order.OrderId))
                    errors.Add("OrderId is required.");

                if (string.IsNullOrWhiteSpace(order.CustomerName))
                    errors.Add("CustomerName is required.");

                if (order.SelectedItemSKUs == null || order.SelectedItemSKUs.Count == 0)
                    errors.Add("SelectedItemSKUs must contain at least one item.");
                else if (order.SelectedItemSKUs.Any(string.IsNullOrWhiteSpace))
                    errors.Add("SelectedItemSKUs cannot contain blank values.");

                if (order.TotalPrice <= 0)
                    errors.Add("TotalPrice must be greater than 0.");

                if (errors.Count > 0)
                {
                    _logger.LogWarning("Order rejected: {Errors}", string.Join(" | ", errors));
                    return await WriteJson(req, HttpStatusCode.BadRequest, new { errors });
                }

                // Serialises the validated order into a JSON string.
                string messageJson = JsonSerializer.Serialize(order);

                // Connecting to the queue.
                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";
                var queueClient = new QueueClient(connectionString, QueueName,
                    new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });

                // Creates the queue the first time it is used.
                await queueClient.CreateIfNotExistsAsync();

                // Places the order on the queue.
                await queueClient.SendMessageAsync(messageJson);

                _logger.LogInformation("Order {OrderId} placed on {Queue}.", order.OrderId, QueueName);

                // 202 Accepted message.
                return await WriteJson(req, HttpStatusCode.Accepted,
                    new { message = "Order accepted and queued for processing.", orderId = order.OrderId });
            }
            catch (RequestFailedException ex)
            {
                // Messafe for storage or queue connection problem 
                _logger.LogError(ex, "Azure Queue error while placing the order.");
                return await WriteJson(req, HttpStatusCode.ServiceUnavailable,
                    new { error = $"Queue unavailable: {ex.ErrorCode} - {ex.Message}" });
            }
            catch (Exception ex)
            {
                // Returning a clean 500 instead of crashing for any other unexpected error.
                _logger.LogError(ex, "Unexpected error while placing the order.");
                return await WriteJson(req, HttpStatusCode.InternalServerError,
                    new { error = $"Error: {ex.Message}" });
            }
        }

        // Builds a response with the given status code and a JSON body.
        private static async Task<HttpResponseData> WriteJson(HttpRequestData req, HttpStatusCode status, object payload)
        {
            var response = req.CreateResponse(status);
            await response.WriteAsJsonAsync(payload);
            return response;
        }
    }
}