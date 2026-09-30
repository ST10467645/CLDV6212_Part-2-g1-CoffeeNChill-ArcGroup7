// Here I made a queue triggered function. It runs by itself whenever a new message arrives on the order-processing-queue.
// For now it only prints the message so I can check that the trigger works.
// The real order processing gets added in the next commits.
// Attribution: the [Function] and [QueueTrigger(..., Connection = "AzureWebJobsStorage")] pattern was adapted from the lecturer's ProcessStudentQuery example and Microsoft Learn (2026).
//https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue-trigger?tabs=python-v2%2Cisolated-process%2Cnodejs-v4%2Cextensionv5&pivots=programming-language-csharp
//[Accessed: 29-Sep-2026].
// The ILogger injection follows the same pattern I used in the Part 1 Blob functions.

//The ?: operator picks one of two values depending on the condition(what the user entered, or the current time).
//Reference: Microsoft, "?: operator - the ternary conditional operator," Microsoft Learn, 2026.
//https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/conditional-operator
//[Accessed: 30-Sep-2026].
//A table column can't hold a list, so the SKUs are joined into one text value, for example "COF-001,PAS-104".
//String.Join puts a separator between each item in the list.
//Reference: Microsoft, "String.Join Method," Microsoft Learn, 2026.
//https://learn.microsoft.com/en-us/dotnet/api/system.string.join
//[Accessed: 30-Sep-2026].
using Azure.Data.Tables;
using CoffeeNChillFunctions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CoffeeNChillFunctions.Functions
{
    public class ProcessOrderQueue
    {
        //The logger so I can use it in the Run method below
        private readonly ILogger _logger;

        //This is the client I use to read and write the Orders table
        private readonly TableClient _ordersTable;

        //The Functions host gives us the logger automatically (dependency injection) so I don't have to create it myself
        public ProcessOrderQueue(ILoggerFactory loggerFactory)
        {
            //Makes a logger ProcessOrderQueue after this class so the logs show where they came from
            _logger = loggerFactory.CreateLogger<ProcessOrderQueue>();

            //Uses Azurite locally, the same connection string as my Part 1 functions.
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";

            _ordersTable = new TableClient(connectionString, "Orders");

            //Creates the Orders table if it isn't there yet
            _ordersTable.CreateIfNotExists();
        }

        //[Function] tells Azure Functions that this method is a function and gives it a name
        [Function("ProcessOrderQueue")]
        public async Task Run(
            //[QueueTrigger] makes this method run every time a new message lands in the queue.
            //Connection = "AzureWebJobsStorage" tells it which storage account to use.
            //The following message gets decoded for us because of the setting in host.json.
            [QueueTrigger("order-processing-queue", Connection = "AzureWebJobsStorage")] string queueMessage)
        {
            //These lines make the message easy to spot in the console when I test
            _logger.LogInformation("=================================");
            _logger.LogInformation("ORDER MESSAGE RECEIVED");
            //Prints the message text so I can check the JSON came through properly
            _logger.LogInformation("Message text: {Message}", queueMessage);
            _logger.LogInformation("=================================");

            //Turns the JSON text into an Order object, capital letters in the names don't matter
            var order = JsonSerializer.Deserialize<Order>(queueMessage, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            //If the order is empty or missing the OrderId or the items we log and throw the error.
            //Throwing makes the message retry and then move to the poison queue.
            if (order == null || string.IsNullOrWhiteSpace(order.OrderId) || order.SelectedItemSKUs == null || order.SelectedItemSKUs.Count == 0)
            {
                _logger.LogError("Unable to process order: it is empty or the OrderId or items are missing.");
                throw new InvalidOperationException("Unable to deserialize a valid order.");
            }

            //Uses the order date for the PartitionKey. If no timestamp was sent it gives the current time.
            //The ?: operator picks one of two values depending on the condition(what the user entered, or the current time).
            //https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/conditional-operator
            DateTime orderDate = order.OrderTimestamp == default ? DateTime.UtcNow : order.OrderTimestamp;
            
            //Builds the row for the Orders table
            var entity = new OrderEntity
            {
                PartitionKey = orderDate.ToString("yyyy-MM-dd"),
                RowKey = order.OrderId,
                CustomerName = order.CustomerName,
                //A table column can't hold a list, so the SKUs are joined into one text value like "COF-001,PAS-104".
                //String.Join puts a separator between each item in the list.
                //https://learn.microsoft.com/en-us/dotnet/api/system.string.join
                SelectedItemSKUs = string.Join(",", order.SelectedItemSKUs),
                TotalPrice = order.TotalPrice,
                OrderTimestamp = orderDate.ToString("o"),
                Status = OrderStatus.Received,
                LastUpdated = DateTime.UtcNow.ToString("o")
            };

            //Upsert inserts the row, or replaces it if the same order is sent again
            await _ordersTable.UpsertEntityAsync(entity);

            _logger.LogInformation("=================================");
            _logger.LogInformation("ORDER SAVED");
            _logger.LogInformation("Order ID: {OrderId}", entity.RowKey);
            _logger.LogInformation("Customer: {Customer}", entity.CustomerName);
            _logger.LogInformation("Status: {Status}", entity.Status);
            _logger.LogInformation("=================================");
        }
    }
}