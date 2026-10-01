// Queue PRODUCER: validates an order, turns it into JSON and places it on order-processing-queue.
// The customer gets an instant 202 Accepted; the order is processed later by ProcessOrderQueue.
//
// Attribution: HTTP trigger and queue producer pattern adapted from the lecturer's examples (IIE, 2026)
// and the HTTP trigger documentation (Microsoft Learn, 2025b); QueueClient and Base64 message encoding
// per the Queues client library documentation (Microsoft Learn, 2026h); trigger-side queue rules
// per the Queue storage trigger documentation (Microsoft Learn, 2026g).
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
        private const string QueueName = "order-processing-queue";

        private readonly ILogger _logger;

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
                // Reading the request body as text.
                string body = await new StreamReader(req.Body).ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(body))
                {
                    return await WriteJson(req, HttpStatusCode.BadRequest,
                        new { errors = new[] { "Request body is empty." } });
                }

                // Converting the JSON text into an Order object.
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

                // Validating every field and collecting all problems so the client can fix them in one go.
                var errors = new List<string>();

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

                // Server-generated values:
                // OrderId looks like ORD-2026-A1B2C3; the timestamp is the current UTC time.
                if (string.IsNullOrWhiteSpace(order.OrderId))
                {
                    order.OrderId = $"ORD-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
                }

                if (order.OrderTimestamp == default)
                {
                    order.OrderTimestamp = DateTime.UtcNow;
                }

                order.CustomerName = order.CustomerName.Trim();

                // Serialising the validated order into a JSON string (the queue message).
                string messageJson = JsonSerializer.Serialize(order);

                // Connecting to the queue.
                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";
                var options = new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 };

                // Failing fast if the queue is unreachable instead of waiting a long time.
                options.Retry.MaxRetries = 1;
                options.Retry.Delay = TimeSpan.FromMilliseconds(500);
                options.Retry.NetworkTimeout = TimeSpan.FromSeconds(5);

                var queueClient = new QueueClient(connectionString, QueueName, options);

                // Creating the queue the first time it is used, then places the order on it.
                await queueClient.CreateIfNotExistsAsync();
                await queueClient.SendMessageAsync(messageJson);

                _logger.LogInformation("Order {OrderId} placed on {Queue}.", order.OrderId, QueueName);

                // 202 Accepted means (received and will be processed later).
                return await WriteJson(req, HttpStatusCode.Accepted, new
                {
                    message = "Order accepted and queued for processing.",
                    orderId = order.OrderId,
                    orderDate = order.OrderTimestamp.ToString("yyyy-MM-dd")
                });
            }
            catch (RequestFailedException ex)
            {
                // Azure Storage error message.
                _logger.LogError(ex, "Azure Queue error while placing the order.");
                return await WriteJson(req, HttpStatusCode.ServiceUnavailable,
                    new { error = $"Queue unavailable: {ex.ErrorCode} - {ex.Message}" });
            }
            catch (Exception ex) when (ex is AggregateException || ex is HttpRequestException ||
                                       ex is TimeoutException || ex is OperationCanceledException)
            {
                // If queue could not be reached at all (for example Azurite is stopped).
                _logger.LogError(ex, "Queue storage is not reachable.");
                return await WriteJson(req, HttpStatusCode.ServiceUnavailable,
                    new { error = "Queue storage is not reachable. Please try again shortly." });
            }
            catch (Exception ex)
            {
                // Returning a clean 500 instead of crashing for any other unexpected error .
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