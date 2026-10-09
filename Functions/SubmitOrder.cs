/*
 * Code Attribution:
 * The HTTP-triggered queue producer pattern used to validate an order,
 * serialise it as JSON and enqueue it for asynchronous processing was
 * adapted from the lecturer-provided course example and Microsoft's
 * official Azure Functions and Azure Queue Storage documentation.
 *
 * Lecturer CLDV6212 course example.
 *
 * Microsoft Learn (2025b). Azure Functions HTTP trigger. [Online]
 * Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger?tabs=python-v2%2Cisolated-process%2Cnodejsv4%2Cfunctionsv2&pivots=programming-language-csharp
 * [Accessed 04 September 2026].
 *
 * Microsoft Learn (2026q). Azure Queue Storage client library for .NET. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/storage.queues-readme?view=azure-dotnet
 * [Accessed 04 September 2026].
 *
 * Microsoft Learn (2026g). Azure Queue storage trigger for Azure Functions. [Online]
 * Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue-trigger
 * [Accessed 29 September 2026].
 */

using Azure;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using CoffeeNChillFunctions.Models;
using CoffeeNChillFunctions.DTOs;
using System.Text;

namespace CoffeeNChillFunctions.Functions
{
    // Defines the Azure Function responsible for accepting a new order and placing it on the order-processing-queue.
    public class SubmitOrder
    {
        // Stores the queue name in one place so that it always matches the queue used by the ProcessOrderQueue trigger.
        private const string QueueName = "order-processing-queue";

        // The logger used to write information, warnings and errors to the console.
        private readonly ILogger _logger;

        // The Functions host provides the logger automatically (dependency injection), the same way as in the Part 1 functions.
        public SubmitOrder(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SubmitOrder>();
        }

        // Exposes this method as an Azure Function that accepts HTTP POST requests through the /orders/queue route.
        [Function("SubmitOrder")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders/queue")] HttpRequestData req)
        {
            try
            {
                // Reads the request body so that the submitted JSON data can be converted into a CreateOrderRequest DTO.
                string body = await new StreamReader(req.Body).ReadToEndAsync();

                // Returns 400 Bad Request when the request body is empty.
                if (string.IsNullOrWhiteSpace(body))
                {
                    return await WriteJson(req, HttpStatusCode.BadRequest,
                        new ValidationErrorResponse("Request body is empty."));
                }

                // Deserialises the JSON request body into a CreateOrderRequest DTO instead of the Order queue message model.
                // PropertyNameCaseInsensitive allows JSON property names to use different capitalisation from the C# model properties.
                CreateOrderRequest? request;
                try
                {
                    request = JsonSerializer.Deserialize<CreateOrderRequest>(body,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException)
                {
                    // Returns 400 Bad Request when the JSON is not valid or a field has the wrong type, for example a price written as text.
                    return await WriteJson(req, HttpStatusCode.BadRequest,
                        new ValidationErrorResponse("Request body is not valid JSON."));
                }

                // Returns 400 Bad Request when the JSON could be read but did not contain an order, for example when the body is just null.
                if (request == null)
                {
                    return await WriteJson(req, HttpStatusCode.BadRequest,
                        new ValidationErrorResponse("Order could not be read."));
                }

                // Validates every field and collects all the problems in one list, so that the client can fix them in one go.
                var errors = new List<string>();

                if (string.IsNullOrWhiteSpace(request.CustomerName))
                    errors.Add("CustomerName is required.");

                if (request.SelectedItemSKUs == null || request.SelectedItemSKUs.Count == 0)
                    errors.Add("SelectedItemSKUs must contain at least one item.");
                else if (request.SelectedItemSKUs.Any(string.IsNullOrWhiteSpace))
                    errors.Add("SelectedItemSKUs cannot contain blank values.");

                if (request.TotalPrice <= 0)
                    errors.Add("TotalPrice must be greater than 0.");

                // Returns 400 Bad Request with every validation problem when at least one field is not valid.
                if (errors.Count > 0)
                {
                    _logger.LogWarning("Order rejected: {Errors}", string.Join(" | ", errors));
                    return await WriteJson(req, HttpStatusCode.BadRequest, new ValidationErrorResponse(errors));
                }

                // Maps the validated request DTO into the Order that is placed on the queue.
                // The mapper generates the order number and the UTC timestamp when the client did not send them.
                Order order = OrderMapper.ToOrder(request);

                // Serialises the Order into a JSON string, because the message that is placed on the queue is plain text.
                string messageJson = JsonSerializer.Serialize(order);

                // Encodes the JSON string as Base64 text before it is placed on the queue.
                string base64Message = Convert.ToBase64String(Encoding.UTF8.GetBytes(messageJson));

                // Reads the storage connection string from the environment, it points to Azurite when the app runs locally.
                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";

                // Base64 encoding is done here in the code (encoding, not encryption). host.json has "messageEncoding": "none",
                // so the queue stores exactly the Base64 text we send and ProcessOrderQueue decodes it again.
                // Attribution:
                // Convert.ToBase64String pattern adapted from Microsoft Learn (2026n).
                var options = new QueueClientOptions { MessageEncoding = QueueMessageEncoding.None };

                // Fails fast when the queue is unreachable instead of making the client wait for a long time.
                options.Retry.MaxRetries = 1;
                options.Retry.Delay = TimeSpan.FromMilliseconds(500);
                options.Retry.NetworkTimeout = TimeSpan.FromSeconds(5);

                // Creates a client for accessing the order-processing-queue in Azure Queue Storage.
                var queueClient = new QueueClient(connectionString, QueueName, options);

                // Ensures that the queue exists before the order is placed on it.
                await queueClient.CreateIfNotExistsAsync();

                // Places the order on the queue, the ProcessOrderQueue trigger picks it up from there.
                await queueClient.SendMessageAsync(base64Message);

                _logger.LogInformation("Order {OrderId} placed on {Queue}.", order.OrderId, QueueName);

                // Returns 202 Accepted to indicate that the order was received and will be processed later.
                // The response DTO includes the order date, which is needed to look the order up in the Orders table.
                return await WriteJson(req, HttpStatusCode.Accepted, OrderMapper.ToQueuedResponse(order));
            }
            catch (RequestFailedException ex)
            {
                // Handles errors returned by Azure Storage and returns a 503 Service Unavailable so that the client knows to try again.
                _logger.LogError(ex, "Azure Queue error while placing the order.");
                return await WriteJson(req, HttpStatusCode.ServiceUnavailable,
                    new ErrorResponse($"Queue unavailable: {ex.ErrorCode} - {ex.Message}"));
            }
            catch (Exception ex) when (ex is AggregateException || ex is HttpRequestException ||
                                       ex is TimeoutException || ex is OperationCanceledException)
            {
                // Handles the case where the queue cannot be reached at all, for example when Azurite is stopped.
                _logger.LogError(ex, "Queue storage is not reachable.");
                return await WriteJson(req, HttpStatusCode.ServiceUnavailable,
                    new ErrorResponse("Queue storage is not reachable. Please try again shortly."));
            }
            catch (Exception ex)
            {
                // Handles unexpected errors and returns a 500 Internal Server Error so that the client receives an appropriate failure response.
                _logger.LogError(ex, "Unexpected error while placing the order.");
                return await WriteJson(req, HttpStatusCode.InternalServerError,
                    new ErrorResponse($"Error: {ex.Message}"));
            }
        }

        // Creates a response with the given status code and writes the given DTO into it as JSON.
        private static async Task<HttpResponseData> WriteJson(HttpRequestData req, HttpStatusCode status, object payload)
        {
            var response = req.CreateResponse(status);
            await response.WriteAsJsonAsync(payload);
            return response;
        }
    }
}