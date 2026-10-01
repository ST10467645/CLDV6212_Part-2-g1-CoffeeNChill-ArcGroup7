// This is my function for updating the status of an order. It runs when someone sends a PUT request only.
//Right now it only finds the order and checks the new status. Saving the new status gets added later.
// attribution: the structure (constructor with the table client, reading the status from the body, catching the 404 error) was adapted from the lecturer's UpdateStudentQueryStatus example.

//The HTTP trigger makes the function run on a web request. Here it only accepts PUT.
//Reference: Microsoft, "Azure Functions HTTP trigger," Microsoft Learn, 2026.
//https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger
//[Accessed: 01-Oct-2026].
//RequestFailedException is what the Azure SDK throws when storage says no like 404 not found.
//Reference: Microsoft, "RequestFailedException Class," Microsoft Learn, 2026.
//https://learn.microsoft.com/en-us/dotnet/api/azure.requestfailedexception
//[Accessed: 01-Oct-2026].
using Azure;
using Azure.Data.Tables;
using CoffeeNChillFunctions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace CoffeeNChillFunctions.Functions
{
    public class UpdateOrderStatus
    {
        //The logger so I can write to the console in the Run method
        private readonly ILogger _logger;

        //The client I use to read the Orders table
        private readonly TableClient _ordersTable;

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
                //Look for the order. If it isn't there, Azure throws a 404 error which I catch below.
                var order = await _ordersTable.GetEntityAsync<OrderEntity>(orderDate, orderId);

                //The new status comes in the body as plain text, for example: Preparing
                string newStatus = await new StreamReader(req.Body).ReadToEndAsync();
                newStatus = newStatus.Trim();

                //400 if the status is not Received, Preparing, Ready or Collected
                if (!OrderStatus.IsValid(newStatus))
                {
                    _logger.LogWarning("Bad status '{NewStatus}' sent for order {OrderId}", newStatus, orderId);

                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("Invalid status. Use Received, Preparing, Ready or Collected.");
                    return response;
                }

                //in later commit i will check the OrderStatus.IsNextStep and save the new status to the table.
                //For now I only send back what I found so I can test the route in Postman.
                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(new
                {
                    message = "Order found. Status not saved yet.",
                    orderId = orderId,
                    currentStatus = order.Value.Status,
                    requestedStatus = newStatus
                });
                return response;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                //The order is not in the table
                _logger.LogWarning("Order {OrderId} on {OrderDate} was not found", orderId, orderDate);

                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteStringAsync("Order not found.");
                return response;
            }
            catch (Exception ex)
            {
                //Anything else I did not expect 
                _logger.LogError(ex, "Something went wrong when updating order {OrderId}", orderId);

                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync($"Error: {ex.Message}");
                return response;
            }
        }
    }
}