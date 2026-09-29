// Code attribution: HTTP trigger + Table Storage insert pattern adapted from
// lecturer provided course example and Microsoft Learn:
// Microsoft, "HTTP trigger for Azure Functions," Microsoft Learn, 2025.
// https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger
// [Accessed: 26-Aug-2026].
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;
using CoffeeNChillFunctions.Models;

namespace CoffeeNChillFunctions.Functions
{
    // Defines the Azure Function responsible for creating a new menu item and storing it in the MenuItems table.
    public class CreateMenuItem
    {
        // Exposes this method as an Azure Function that accepts HTTP POST requests through the /menu route.
        [Function("CreateMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "menu")] HttpRequestData req)
        {
            // Creates the HTTP response that will be returned to the client.
            var response = req.CreateResponse();
            try
            {
                // Reads the request body so that the submitted JSON data can be converted into a MenuItem object.
                string body = await new StreamReader(req.Body).ReadToEndAsync();

                // Deserialises the JSON request body into a MenuItem object.
                // PropertyNameCaseInsensitive allows JSON property names to use different capitalisation from the C# model properties.
                var item = JsonSerializer.Deserialize<MenuItem>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Validates the required menu item fields before attempting to store the item in Table Storage.
                if (item == null || string.IsNullOrWhiteSpace(item.PartitionKey) ||
                    string.IsNullOrWhiteSpace(item.RowKey) || string.IsNullOrWhiteSpace(item.Name))
                {
                    // Returns 400 Bad Request when required menu item information has not been provided.
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("Category, SKU (RowKey) and Name are required.");
                    return response;
                }

                // Creates a client for accessing the MenuItems table in Azure Table Storage.
                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";
                var table = new TableClient(connectionString, "MenuItems");

                // Ensures that the MenuItems table exists before inserting the new item.
                await table.CreateIfNotExistsAsync();


                // Adds the validated menu item as a new entity in Table Storage.
                await table.AddEntityAsync(item);

                // Returns 201 Created to indicate that the menu item was successfully added.
                response.StatusCode = HttpStatusCode.Created;
                await response.WriteStringAsync("Menu item created successfully!");
                return response;
            }
            catch (RequestFailedException ex)
            {
                // Handles errors returned by Azure Table Storage and returns the corresponding Azure status code to the client.
                response.StatusCode = (HttpStatusCode)ex.Status;
                await response.WriteStringAsync($"Azure Table Error: {ex.ErrorCode} - {ex.Message}");
                return response;
            }
            catch (Exception ex)
            {
                // Handles unexpected errors and returns a 500 Internal Server Error so that the client receives an appropriate failure response.
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync($"Error: {ex.Message}");
                return response;
            }
        }
    }
}