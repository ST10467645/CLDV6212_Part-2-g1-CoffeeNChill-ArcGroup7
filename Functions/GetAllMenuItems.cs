/*
 * Code Attribution:
 * The Azure Table Storage query pattern used to retrieve all menu items
 * was adapted from the lecturer-provided course example and Microsoft's
 * official Azure Tables client library documentation.
 *
 * Lecturer CLDV6212 course example.
 *
 * Microsoft Learn (2026c). Azure Tables client library for .NET - version 12.12.0. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
 * [Accessed: 27-Aug-2026].
 */

using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using CoffeeNChillFunctions.Models;
using CoffeeNChillFunctions.DTOs;

namespace CoffeeNChillFunctions.Functions
{
    // Defines the Azure Function responsible for retrieving all menu items.
    public class GetAllMenuItems
    {
        // Exposes this method as an Azure Function that can be accessed through an HTTP GET request at the /menu route.
        [Function("GetAllMenuItems")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menu")] HttpRequestData req)
        {
            // Creates the HTTP response with a successful 200 OK status code.
            var response = req.CreateResponse(HttpStatusCode.OK);

            // Creates a client for accessing the MenuItems table in Azure Table Storage.
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";
            var table = new TableClient(connectionString, "MenuItems");

            // Ensures that the required table exists before attempting to query it.
            await table.CreateIfNotExistsAsync();

            // Stores the menu items retrieved from Table Storage.
            // The list now holds MenuItemResponse DTOs instead of the MenuItem table entities.
            var items = new List<MenuItemResponse>();

            // Non-simultaneously retrieves each MenuItem entity from the table and adds it to the results list.
            await foreach (var item in table.QueryAsync<MenuItem>())
                // Converts each MenuItem entity into a MenuItemResponse DTO using the mapper, so that the ETag and Timestamp are not returned to the client.
                items.Add(MenuItemMapper.ToResponse(item));

            // Serialises the menu items as JSON and adds them to the response body.
            await response.WriteAsJsonAsync(items);

            // Returns the completed response to the client.
            return response;
        }
    }
}