/*
 * Code Attribution:
 * The filtered Azure Table Storage query pattern used to retrieve menu
 * items by category was adapted from the lecturer-provided course example
 * and Microsoft's official Azure Table Storage documentation.
 *
 * Lecturer CLDV6212 course example.
 *
 * Microsoft Learn (2022). What is Azure Table storage?. [Online]
 * Available at: https://learn.microsoft.com/en-us/azure/storage/tables/table-storage-overview
 * [Accessed 04 September 2026].
 */
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using CoffeeNChillFunctions.Models;
using CoffeeNChillFunctions.DTOs;

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
            // The list now holds MenuItemResponse DTOs instead of the MenuItem table entities.
            var items = new List<MenuItemResponse>();

            // Non-simultaneously queries Table Storage for entities whose PartitionKey matches the supplied category.
            await foreach (var item in table.QueryAsync<MenuItem>(x => x.PartitionKey == category))
                // Converts each matching MenuItem entity into a MenuItemResponse DTO using the mapper, so that the ETag and Timestamp are not returned to the client.
                items.Add(MenuItemMapper.ToResponse(item));

            // Creates a successful HTTP response after the query has completed.
            var response = req.CreateResponse(HttpStatusCode.OK);

            // Serialises the matching menu items as JSON.
            // If no items match the category, an empty array is returned rather than an error.
            await response.WriteAsJsonAsync(items);
            return response;
        }
    }
}