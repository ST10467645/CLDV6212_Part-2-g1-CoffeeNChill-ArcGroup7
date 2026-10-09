/*
 * Code Attribution:
 * The Azure Queue Storage trigger pattern was adapted from the lecturer-provided
 * ProcessStudentQuery example and Microsoft's official Azure Functions documentation.
 * The ILogger injection pattern follows my Part 1 Blob functions.
 *
 * CLDV6212 lecturer-provided ProcessStudentQuery course example.
 *
 * Microsoft Learn (2026g). Azure Queue storage trigger for Azure Functions. [Online]
 * Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue-trigger
 * [Accessed 29 September 2026].
 *
 * The conditional operator, String.Join, Task.Delay, UpsertEntityAsync,
 * Environment.GetEnvironmentVariable, Int32.TryParse, Convert.FromBase64String
 * and Encoding.GetString methods were implemented with reference to Microsoft's
 * official .NET documentation.
 *
 * Microsoft Learn (2026h). Conditional operator (?:). [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/conditional-operator
 * [Accessed 30 September 2026].
 *
 * Microsoft Learn (2026i). String.Join Method. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/system.string.join
 * [Accessed 30 September 2026].
 *
 * Microsoft Learn (2026j). Task.Delay Method. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.delay
 * [Accessed 01 October 2026].
 *
 * Microsoft Learn (2026k). TableClient.UpsertEntityAsync Method. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/azure.data.tables.tableclient.upsertentityasync
 * [Accessed 01 October 2026].
 *
 * Microsoft Learn (2026l). Azure Queue storage trigger for Azure Functions - Poison messages. [Online]
 * Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue-trigger
 * [Accessed 01 October 2026].
 *
 * Microsoft Learn (2026m). Environment.GetEnvironmentVariable Method. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/system.environment.getenvironmentvariable
 * [Accessed 01 October 2026].
 *
 * Microsoft Learn (2026n). Int32.TryParse Method. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/system.int32.tryparse
 * [Accessed 05 October 2026].
 *
 * Microsoft Learn (2026o). Convert.FromBase64String Method. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/system.convert.frombase64string
 * [Accessed 08 October 2026].
 *
 * Microsoft Learn (2026p). Encoding.GetString Method. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/system.text.encoding.getstring
 * [Accessed 08 October 2026].
 */

// NOTE:
// The lecturer said we can use a timer for the waits, but I used Task.Delay
// because it already does the same job. It pauses the function for 30 seconds
// without blocking the thread, and the whole order stays inside one queue message,
// so the status changes still go Received > Preparing > Ready > Collected.
// A timer would add extra code to do the same thing.
//
// I also decode the Base64 message in my own GetJson method instead of copying
// the lecturer's code exactly because my host.json and the producer can send
// either plain JSON or Base64, allowing the function to work with both.


using Azure.Data.Tables;
using CoffeeNChillFunctions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;

namespace CoffeeNChillFunctions.Functions
{
    public class ProcessOrderQueue
    {
        //The logger so I can use it in the Run method below
        private readonly ILogger _logger;

        //This is the client I use to read and write the Orders table
        private readonly TableClient _ordersTable;

        //How many seconds the order waits before the next status. It comes from STATUS_STEP_DELAY_SECONDS.
        //int.TryParse gives false if the setting is missing or is not a number so then I use 30.
        // Docker Compose sets it to 30 seconds (default is also 30).
        private static readonly int StepDelaySeconds = int.TryParse(Environment.GetEnvironmentVariable("STATUS_STEP_DELAY_SECONDS"), out var seconds) && seconds >= 0 ? seconds : 30;

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

            //Test switch for the poison queue demo. It is false unless I set ENABLE_POISON_TEST to true.
            //When it is true the function breaks on purpose so the message ends up in order-processing-queue-poison.
            string poisonTest = Environment.GetEnvironmentVariable("ENABLE_POISON_TEST") ?? "false";

            //checking if the poison test is true then the following happen:
            if (string.Equals(poisonTest, "true", StringComparison.OrdinalIgnoreCase))
            {
                //tells the user exactly which message went bad
                _logger.LogError("TEST ERROR - forcing poison queue for message: {Message}", queueMessage);
                throw new InvalidOperationException("TEST ERROR - forcing poison queue");
            }

            //Decode the message to JSON first, then turn the JSON into an Order object
            string json = GetJson(queueMessage);
            var order = JsonSerializer.Deserialize<Order>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            //If the order is empty or missing the OrderId or the items we log and throw the error.
            //Throwing makes the message retry and then move to the poison queue.
            if (order == null || string.IsNullOrWhiteSpace(order.OrderId) || order.SelectedItemSKUs == null || order.SelectedItemSKUs.Count == 0)
            {
                //tells the user exactly which message went bad
                _logger.LogError("Unable to process order, sending it to the poison queue. Message: {Message}", queueMessage);
                throw new InvalidOperationException("Unable to deserialize a valid order.");
            }

            //Uses the order date for the PartitionKey. If no timestamp was sent it gives the current time.
            //The ?: operator picks one of two values depending on the condition(what the user entered, or the current time).
            DateTime orderDate = order.OrderTimestamp == default ? DateTime.UtcNow : order.OrderTimestamp;

            //Builds the row for the Orders table
            var entity = new OrderEntity
            {
                PartitionKey = orderDate.ToString("yyyy-MM-dd"),
                RowKey = order.OrderId,
                CustomerName = order.CustomerName,
                //A table column can't hold a list, so the SKUs are joined into one text value like "COF-001,PAS-104".
                //String.Join puts a separator between each item in the list.
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

            //shows how many seconds the order is waiting for
            _logger.LogInformation("Order {OrderId}: waiting {Seconds}s for processing...", entity.RowKey, StepDelaySeconds);

            //Now I pretend the barista is busy with the order.
            //I wait a few seconds, then change the status in the Orders table.
            //It goes in this order: Received > Preparing > Ready > Collected
            await Task.Delay(TimeSpan.FromSeconds(StepDelaySeconds));
            await ChangeStatusAsync(entity, OrderStatus.Preparing);

            await Task.Delay(TimeSpan.FromSeconds(StepDelaySeconds));
            await ChangeStatusAsync(entity, OrderStatus.Ready);

            await Task.Delay(TimeSpan.FromSeconds(StepDelaySeconds));
            await ChangeStatusAsync(entity, OrderStatus.Collected);

            _logger.LogInformation("=================================");
            _logger.LogInformation("ORDER FINISHED: {OrderId} is now {Status}", entity.RowKey, entity.Status);
            _logger.LogInformation("=================================");
        }

        //I made this method so I don't have to copy and paste the same update code three times.
        //It changes the status, updates LastUpdated, saves the row and writes a log line.
        private async Task ChangeStatusAsync(OrderEntity entity, string newStatus)
        {
            //Keep the old status so the log can say what it changed from
            string oldStatus = entity.Status;

            entity.Status = newStatus;
            entity.LastUpdated = DateTime.UtcNow.ToString("o");

            //Upsert saves the changed row back into the Orders table
            await _ordersTable.UpsertEntityAsync(entity);

            _logger.LogInformation("STATUS CHANGED: Order {OrderId} went from {OldStatus} to {NewStatus}", entity.RowKey, oldStatus, newStatus);
        }

        //Queue messages travel as Base64 text. 
        // host.json has "messageEncoding": "none", so we receive the Base64 text exactly as SubmitOrder sent it
        // and decode it ourselves in GetJson below.
        private string GetJson(string queueMessage)
        {
            string text = queueMessage.Trim();

            //Already plain JSON, nothing to decode
            if (text.StartsWith("{"))
            {
                return text;
            }

            try
            {
                //Base64 text > bytes > UTF-8 text
                byte[] jsonBytes = Convert.FromBase64String(text);
                string json = Encoding.UTF8.GetString(jsonBytes);
                _logger.LogInformation("Decoded JSON: {Json}", json);
                return json;
            }
            catch (FormatException)
            {
                //Not Base64 either. The Deserialize step below will fail, so the message goes to the poison queue.
                _logger.LogError("Message is neither JSON nor valid Base64.");
                return text;
            }
        }
    }
}