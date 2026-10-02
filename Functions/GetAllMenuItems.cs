// Attribution: Table Storage query pattern adapted from the lecturer's course example (IIE, 2026)
// and the Azure Tables client library documentation (Microsoft Learn, 2026c).
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using CoffeeNChillFunctions.Models;

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
            var items = new List<MenuItem>();

            // Non-simultaneously retrieves each MenuItem entity from the table and adds it to the results list.
            await foreach (var item in table.QueryAsync<MenuItem>())
                items.Add(item);

            // Serialises the menu items as JSON and adds them to the response body.
            await response.WriteAsJsonAsync(items);

            // Returns the completed response to the client.
            return response;
        }
    }
}