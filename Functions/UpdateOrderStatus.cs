/*
 * Code Attribution:
 * The order status update structure, including the constructor with the
 * Azure Table Storage client, reading the requested status from the HTTP
 * request body, handling 404 errors and updating the entity using its ETag,
 * was adapted from the lecturer-provided UpdateStudentQueryStatus example.
 *
 * IIE (2026). CLDV6212 lecturer-provided UpdateStudentQueryStatus course example.
 *
 * Microsoft Learn (2025b). Azure Functions HTTP trigger. [Online]
 * Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger
 * [Accessed 01 October 2026].
 *
 * Microsoft Learn (2026r). RequestFailedException Class. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/azure.requestfailedexception
 * [Accessed 01 October 2026].
 *
 * Microsoft Learn (2026s). JsonSerializer.Deserialize Method. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializer.deserialize
 * [Accessed 05 October 2026].
 *
 * Microsoft Learn (2026t). StreamReader.ReadToEndAsync Method. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/system.io.streamreader.readtoendasync
 * [Accessed 05 October 2026].
 */

using Azure;
using Azure.Data.Tables;
using CoffeeNChillFunctions.Models;
using CoffeeNChillFunctions.DTOs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace CoffeeNChillFunctions.Functions
{
    public class UpdateOrderStatus
    {
        //The logger so I can write to the console in the Run method
        private readonly ILogger _logger;

        //The client I use to read and update the Orders table
        private readonly TableClient _ordersTable;

        //The JSON body the client sends, for example {"Status":"Preparing"}
        private class StatusRequest
        {
            public string? Status { get; set; }
        }

        public UpdateOrderStatus(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UpdateOrderStatus>();

            //Same connection string as my other functions, it points to Azurite when I test locally
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";

            _ordersTable = new TableClient(connectionString, "Orders");

            //Makes the table if it isn't there yet so the function doesn't crash on a fresh Azurite
            _ordersTable.CreateIfNotExists();
        }

        //The route has the order date and the order id in it.
        //They match the PartitionKey and RowKey in the Orders table.
        [Function("UpdateOrderStatus")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "orders/{orderDate}/{orderId}/status")] HttpRequestData req,
            string orderDate,
            string orderId)
        {
            var response = req.CreateResponse();

            try
            {
                //Read the body as JSON so the Postman tests can send {"Status":"Preparing"}.
                //400 if it is empty.
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                if (string.IsNullOrWhiteSpace(body))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;

                    //The error replies use the ErrorResponse DTO so they all have the same shape { "error": "..." }.
                    await response.WriteAsJsonAsync(new ErrorResponse("Request body is empty. Send {\"Status\":\"Preparing\"}."));
                    return response;
                }

                //Turn the JSON text into a StatusRequest, 400 if the JSON is broken.
                StatusRequest? request;
                try
                {
                    request = JsonSerializer.Deserialize<StatusRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteAsJsonAsync(new ErrorResponse("Request body is not valid JSON."));
                    return response;
                }

                string newStatus = request?.Status?.Trim() ?? "";

                //400 if the status is not Received, Preparing, Ready or Collected
                if (!OrderStatus.IsValid(newStatus))
                {
                    _logger.LogWarning("Bad status '{NewStatus}' sent for order {OrderId}", newStatus, orderId);

                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteAsJsonAsync(new ErrorResponse("Status must be one of: Received, Preparing, Ready, Collected."));
                    return response;
                }

                //Look for the order. If it isn't there, Azure throws a 404 error which I catch below.
                var order = await _ordersTable.GetEntityAsync<OrderEntity>(orderDate, orderId);

                //400 if the new status is not the next step in the order( Received > Preparing > Ready > Collected)
                //For example Received can only go to Preparing, and Collected can't go anywhere.
                if (!OrderStatus.IsNextStep(order.Value.Status, newStatus))
                {
                    _logger.LogWarning("Order {OrderId} can't go from {OldStatus} to {NewStatus}", orderId, order.Value.Status, newStatus);

                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteAsJsonAsync(new ErrorResponse($"Not allowed. {order.Value.Status} can't go to {newStatus}."));
                    return response;
                }

                //Keep the old status for the log, then change the row
                string oldStatus = order.Value.Status;
                order.Value.Status = newStatus;
                order.Value.LastUpdated = DateTime.UtcNow.ToString("o");

                //Saves the row. The ETag makes sure nobody else changed it while I was working on it.
                await _ordersTable.UpdateEntityAsync(order.Value, order.Value.ETag);

                _logger.LogInformation("STATUS UPDATED: Order {OrderId} went from {OldStatus} to {NewStatus}", orderId, oldStatus, newStatus);

                //Reply with OrderResponse, the same shape the GET functions use (OrderId, Status, OrderDate)
                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(OrderResponse.From(order.Value));
                return response;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                //The order is not in the table
                _logger.LogWarning("Order {OrderId} on {OrderDate} was not found", orderId, orderDate);

                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteAsJsonAsync(new ErrorResponse($"Order '{orderId}' for date '{orderDate}' not found."));
                return response;
            }
            catch (Exception ex)
            {
                //Anything else I did not expect
                _logger.LogError(ex, "Something went wrong when updating order {OrderId}", orderId);

                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteAsJsonAsync(new ErrorResponse($"Error: {ex.Message}"));
                return response;
            }
        }
    }
}