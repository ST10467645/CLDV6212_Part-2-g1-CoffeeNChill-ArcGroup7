// Code attribution: HTTP DELETE + Table Storage delete pattern adapted from
// lecturer-provided course example and Microsoft's
// official documentation:
// Microsoft, "Azure Tables client library for .NET," Microsoft Learn, 6 May 2025. [Online]
// https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
// [Accessed: 28-Aug-2026].
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace CoffeeNChillFunctions.Functions
{
    // Defines the Azure Function responsible for deleting a menu item from the MenuItems table in Table Storage.
    public class DeleteMenuItem
    {
        // Exposes this method as an Azure Function that handles HTTP DELETE requests using the category and id values supplied in the route.
        [Function("DeleteMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "menu/{category}/{id}")] HttpRequestData req,
            string category, string id)
        {
            // Creates the HTTP response that will be returned to the client.
            var response = req.CreateResponse();
            try
            {
                // Creates a client for accessing the MenuItems table in Azure Table Storage.
                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";
                var table = new TableClient(connectionString, "MenuItems");

                // Checks it exists first, so we can return a proper 404.
                try
                {
                    // Checks whether the requested menu item exists using its partition key (category) and row key (id).
                    await table.GetEntityAsync<Azure.Data.Tables.TableEntity>(category, id);
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    // Returns a 404 Not Found response when the requested menu item does not exist in Table Storage.
                    response.StatusCode = HttpStatusCode.NotFound;
                    await response.WriteStringAsync($"Menu item '{id}' in category '{category}' not found.");
                    return response;
                }


                // Deletes the menu item using its partition key and row key.
                await table.DeleteEntityAsync(category, id);

                // Returns a successful response to confirm that the item was deleted.
                response.StatusCode = HttpStatusCode.OK;
                await response.WriteStringAsync("Menu item deleted successfully!");
                return response;
            }
            catch (RequestFailedException ex)
            {

                // Handles errors returned by Azure Table Storage and uses the Azure error status code in the HTTP response.
                response.StatusCode = (HttpStatusCode)ex.Status;
                await response.WriteStringAsync($"Azure Table Error: {ex.ErrorCode} - {ex.Message}");
                return response;
            }
            catch (Exception ex)
            {
                // Handles unexpected errors and returns a 500 Internal Server Error instead of allowing the function to fail without a response.
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync($"Error: {ex.Message}");
                return response;
            }
        }
    }
}