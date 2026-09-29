// Here I made a queue triggered function. It runs by itself whenever a new message arrives on the order-processing-queue.
// For now it only prints the message so I can check that the trigger works.
// The real order processing gets added in the next commits.
// Attribution: the [Function] and [QueueTrigger(..., Connection = "AzureWebJobsStorage")] pattern was adapted from the lecturer's ProcessStudentQuery example and Microsoft Learn (2026).
//https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue-trigger?tabs=python-v2%2Cisolated-process%2Cnodejs-v4%2Cextensionv5&pivots=programming-language-csharp
// The ILogger injection follows the same pattern I used in the Part 1 Blob functions.
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CoffeeNChillFunctions.Functions
{
    public class ProcessOrderQueue
    {
        //The logger so I can use it in the Run method below
        private readonly ILogger _logger;

        //The Functions host gives us the logger automatically (dependency injection) so I don't have to create it myself
        public ProcessOrderQueue(ILoggerFactory loggerFactory)
        {
            //Makes a logger ProcessOrderQueue after this class so the logs show where they came from
            _logger = loggerFactory.CreateLogger<ProcessOrderQueue>();
        }

        //[Function] tells Azure Functions that this method is a function and gives it a name
        [Function("ProcessOrderQueue")]
        public void Run(
            //[QueueTrigger] makes this method run every time a new message lands in the queue.
            //Connection = "AzureWebJobsStorage" tells it which storage account to use.
            //The following message gets decoded for us because of the setting in host.json.
            [QueueTrigger("order-processing-queue", Connection = "AzureWebJobsStorage")]
            string queueMessage)
        {
            //These lines make the message easy to spot in the console when I test
            _logger.LogInformation("=================================");
            _logger.LogInformation("ORDER MESSAGE RECEIVED");
            //Prints the message text so I can check the JSON came through properly
            _logger.LogInformation("Message text: {Message}", queueMessage);
            _logger.LogInformation("=================================");
        }
    }
}