// Code attribution: directly adapted from lecturer-provided "Method for put
// and delete" course example, using the same PUT +
// ETag concurrency pattern, and Microsoft's official documentation:
// Microsoft, "Azure Tables client library for .NET," Microsoft Learn,6 May 2025.
// https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
// [Accessed: 28-Aug-2026].
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;
using CoffeeNChillFunctions.Models;

namespace CoffeeNChillFunctions.Functions
{
    // Defines the Azure Function responsible for updating an existing menu item stored in the MenuItems table.
    public class UpdateMenuItem
    {
        // Exposes this method as an Azure Function that handles HTTP PUT requests using the category and id supplied in the route.
        [Function("UpdateMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "menu/{category}/{id}")] HttpRequestData req,
            string category, string id)
        {
            // Creates the HTTP response that will be returned to the client.
            var response = req.CreateResponse();
            try
            {
                // Reads the request body so that the submitted JSON data
                // can be converted into a MenuItem object.
                string body = await new StreamReader(req.Body).ReadToEndAsync();

                // Deserialises the JSON request body into a MenuItem object while allowing different capitalisation of property names.
                var updated = JsonSerializer.Deserialize<MenuItem>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Validates that the request contains valid JSON that could be converted into a MenuItem object.
                if (updated == null)
                {
                    // Returns 400 Bad Request when the submitted JSON is invalid.
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("Invalid JSON.");
                    return response;
                }

                // Creates a client for accessing the MenuItems table in Azure Table Storage.
                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";
                var table = new TableClient(connectionString, "MenuItems");

                // Stores the existing menu item so that it can be checked before the update is performed.
                Response<MenuItem> existing;
                try
                {
                    // Retrieves the existing menu item using its partition key and row key.
                    existing = await table.GetEntityAsync<MenuItem>(category, id);
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    // Returns 404 Not Found when the requested menu item does not exist in Table Storage.
                    response.StatusCode = HttpStatusCode.NotFound;
                    await response.WriteStringAsync("Menu item not found.");
                    return response;
                }

                // Ensures that the updated entity uses the same partition key and row key as the existing menu item.
                updated.PartitionKey = category;
                updated.RowKey = id;

                // Copies the existing ETag to support optimistic concurrency and ensure the update is based on the current version of the entity.
                updated.ETag = existing.Value.ETag;

                // Replaces the existing entity with the updated values while using the ETag to help prevent conflicting updates.
                await table.UpdateEntityAsync(updated, updated.ETag, TableUpdateMode.Replace);

                // Returns 200 OK to indicate that the menu item was successfully updated.
                response.StatusCode = HttpStatusCode.OK;
                await response.WriteStringAsync("Menu item updated successfully!");
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