/*
 * Code Attribution:
 * The HTTP trigger and Azure Table Storage insert patterns used in this
 * function were adapted from the lecturer-provided course example and
 * Microsoft's official documentation.
 *
 * Microsoft Learn (2025b). Azure Functions HTTP trigger. [Online]
 * Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger?tabs=python-v2%2Cisolated-process%2Cnodejsv4%2Cfunctionsv2&pivots=programming-language-csharp
 * [Accessed 26-Aug-2026].
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
                // The JSON is now read into a MenuItemRequest DTO instead of the MenuItem table entity, so that the shape of the incoming request stays separate from the shape of the stored data.
                MenuItemRequest? dto;
                try
                {
                    dto = JsonSerializer.Deserialize<MenuItemRequest>(body,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException)
                {
                    // Returns 400 Bad Request when the request body is empty or is not valid JSON, instead of letting the exception cause a 500 Internal Server Error.
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("Invalid JSON.");
                    return response;
                }

                // Validates the required menu item fields before attempting to store the item in Table Storage.
                if (dto == null || string.IsNullOrWhiteSpace(dto.Category) ||
                    string.IsNullOrWhiteSpace(dto.Sku) || string.IsNullOrWhiteSpace(dto.Name))
                {
                    // Returns 400 Bad Request when required menu item information has not been provided.
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("Category, SKU (RowKey) and Name are required.");
                    return response;
                }

                // Validates that the price has not been entered as a negative number before the item is stored.
                if (dto.Price < 0)
                {
                    // Returns 400 Bad Request when the price is negative.
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("Price cannot be negative.");
                    return response;
                }

                // Maps the validated MenuItemRequest DTO into a MenuItem entity so that it can be stored in the MenuItems table.
                MenuItem item = MenuItemMapper.ToEntity(dto);

                // Creates a client for accessing the MenuItems table in Azure Table Storage.
                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";
                var table = new TableClient(connectionString, "MenuItems");

                // Ensures that the MenuItems table exists before inserting the new item.
                await table.CreateIfNotExistsAsync();


                // Adds the validated menu item as a new entity in Table Storage.
                await table.AddEntityAsync(item);

                // Returns 201 Created to indicate that the menu item was successfully added.
                response.StatusCode = HttpStatusCode.Created;

                // Returns the created menu item as a MenuItemResponse DTO instead of a plain message, so that the client can see the saved values without the ETag and Timestamp storage details.
                await response.WriteAsJsonAsync(MenuItemMapper.ToResponse(item));
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