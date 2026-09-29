// Code attribution: filtered Table Storage query pattern adapted from
// lecturer-provided course example (CLDV6212, IIE, 2026) and Microsoft's
// official documentation:
// Microsoft, "Understand the Azure Table storage data model," Microsoft Learn, 2025. 
// https://learn.microsoft.com/en-us/azure/storage/tables/table-storage-overview
// [Accessed: 27-Aug-2026].
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using CoffeeNChillFunctions.Models;

namespace CoffeeNChillFunctions.Functions
{
    // Defines the Azure Function responsible for retrieving menu items belonging to a specific category.
    public class GetMenuItemsByCategory
    {

        // Exposes this method as an Azure Function that handles HTTP GET requests using the category supplied in the route.
        [Function("GetMenuItemsByCategory")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menu/category/{category}")] HttpRequestData req,
            string category)
        {
            // Validates that a category value has been provided before attempting to query the Table Storage database.
            if (string.IsNullOrWhiteSpace(category))
            {
                // Returns 400 Bad Request when the category is missing or empty.
                var badReq = req.CreateResponse(HttpStatusCode.BadRequest);
                await badReq.WriteStringAsync("Category must be provided in the route.");
                return badReq;
            }

            // Creates a client for accessing the MenuItems table in Azure Table Storage.
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";
            var table = new TableClient(connectionString, "MenuItems");

            // Ensures that the MenuItems table exists before performing the query.
            await table.CreateIfNotExistsAsync();

            // Creates a list to store the menu items matching the requested category.
            var items = new List<MenuItem>();

            // Non-simultaneously queries Table Storage for entities whose PartitionKey matches the supplied category.
            await foreach (var item in table.QueryAsync<MenuItem>(x => x.PartitionKey == category))
                items.Add(item);

            // Creates a successful HTTP response after the query has completed.
            var response = req.CreateResponse(HttpStatusCode.OK);

            // Serialises the matching menu items as JSON.
            // If no items match the category, an empty array is returned rather than an error.
            await response.WriteAsJsonAsync(items); 
            return response;
        }
    }
}
