/*
 * Code Attribution:
 * The HTTP PUT update pattern and ETag concurrency handling used in this
 * function were adapted from the lecturer-provided "Method for put and
 * delete" course example and Microsoft's official Azure Tables client
 * library documentation.
 *
 * IIE (2026). CLDV6212 lecturer-provided "Method for put and delete" course example.
 *
 * Microsoft Learn (2026c). Azure Tables client library for .NET - version 12.12.0. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
 * [Accessed 04 September 2026].
 */

using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;
using CoffeeNChillFunctions.Models;
using CoffeeNChillFunctions.DTOs;

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
                // The JSON is now read into a MenuItemUpdateRequest DTO, so that only the values that were sent are changed and the other values of the menu item stay as they were.
                MenuItemUpdateRequest? updateDto;
                try
                {
                    updateDto = JsonSerializer.Deserialize<MenuItemUpdateRequest>(body,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException)
                {
                    // Treats a body that is empty or is not valid JSON the same as a missing request, so that it returns 400 instead of a 500 Internal Server Error.
                    updateDto = null;
                }

                // Validates that the request contains valid JSON that could be converted into a MenuItem object.
                if (updateDto == null)
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

                // Validates that the new price, when one was sent, has not been entered as a negative number.
                if (updateDto.Price.HasValue && updateDto.Price.Value < 0)
                {
                    // Returns 400 Bad Request when the price is negative.
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("Price cannot be negative.");
                    return response;
                }

                // Starts from the existing menu item and lets the mapper copy only the values that were sent onto it.
                MenuItem updated = existing.Value;
                MenuItemMapper.ApplyUpdate(updated, updateDto);

                // Ensures that the updated entity uses the same partition key and row key as the existing menu item.
                updated.PartitionKey = category;
                updated.RowKey = id;

                // Copies the existing ETag to support optimistic concurrency and ensure the update is based on the current version of the entity.
                updated.ETag = existing.Value.ETag;

                // Replaces the existing entity with the updated values while using the ETag to help prevent conflicting updates.
                await table.UpdateEntityAsync(updated, updated.ETag, TableUpdateMode.Replace);

                // Returns 200 OK to indicate that the menu item was successfully updated.
                response.StatusCode = HttpStatusCode.OK;

                // Returns the updated menu item as a MenuItemResponse DTO instead of a plain message, so that the client can see the saved values.
                await response.WriteAsJsonAsync(MenuItemMapper.ToResponse(updated));
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