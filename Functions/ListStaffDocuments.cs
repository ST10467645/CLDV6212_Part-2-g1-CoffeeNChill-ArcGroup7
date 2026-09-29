// Lists all stored operational files in the "staff-docs" blob container, returning file name, size, and last modified date for each.
// Code attribution: blob listing pattern adapted from lecturer-provided blob storage example and Microsoft's official
// Azure Blob Storage documentation:
// Microsoft, "Introduction to Azure Blob Storage," Microsoft Learn, 2025.
// https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blobs-introduction
// [Accessed: 28-Aug-2026].
using Azure;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace CoffeeNChillFunctions.Functions
{
    public class ListStaffDocuments
    {
        private readonly ILogger _logger;

        public ListStaffDocuments(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ListStaffDocuments>();
        }

        [Function("ListStaffDocuments")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents")] HttpRequestData req)
        {
            _logger.LogInformation("Listing staff documents started.");

            var response = req.CreateResponse();
            try
            {
                //Connects to the staff-docs container, creates one if it hasn't been created yet
                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";
                var containerClient = new BlobContainerClient(connectionString, "staff-docs");
                await containerClient.CreateIfNotExistsAsync();

                var files = new List<object>();
                //Loops through all the files in the container and gets their name, size and last modified date
                await foreach (var blobItem in containerClient.GetBlobsAsync())
                {
                    files.Add(new
                    {
                        FileName = blobItem.Name,
                        SizeInBytes = blobItem.Properties.ContentLength,
                        LastModified = blobItem.Properties.LastModified
                    });
                }

                //Logs how many files were found and sends the list back as JSON
                _logger.LogInformation($"Found {files.Count} staff document(s).");
                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(files);
                return response;
            }
            //Catches Azure-specific storage errors in a different catch so we can log the Azure error code like (ContainerNotFound) rather than a generic message.
            catch (RequestFailedException ex)
            {
                _logger.LogError($"Azure Blob Storage error listing documents: {ex.ErrorCode} - {ex.Message}");
                response.StatusCode = (HttpStatusCode)ex.Status;
                await response.WriteStringAsync($"Azure Blob Storage Error: {ex.ErrorCode} - {ex.Message}");
                return response;
            }
            //Catches any other unexpected errors that aren't Azure related
            catch (Exception ex)
            {
                _logger.LogError($"Error listing staff documents: {ex.Message}");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync($"Error: {ex.Message}");
                return response;
            }
        }
    }
}