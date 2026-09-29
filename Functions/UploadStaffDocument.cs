// Code attribution: multipart-form parsing pattern adapted from Microsoft's MultipartReader documentation:
// Microsoft, "MultipartReader Class," Microsoft Learn, 2025.
// https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.webutilities.multipartreader
// [Accessed: 26-Aug-2026].
// Blob upload pattern adapted from lecturer provided blob storage example and Microsoft Azure Blob Storage documentation:
// Microsoft, "Introduction to Azure Blob Storage," Microsoft Learn, 2025.
// https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blobs-introduction
// [Accessed: 26-Aug-2026].
// I was having problems with uploading in postman so the following helped me:
// Content-Type resolution on upload adapted from Microsoft's Azure Blob Storage documentation:
// Microsoft, "BlobHttpHeaders Class," Microsoft Learn, 2025.
// https://learn.microsoft.com/en-us/dotnet/api/azure.storage.blobs.models.blobhttpheaders?view=azure-dotnet
// [Accessed: 04-Sep-2026].
// Microsoft, "BlobUploadOptions.HttpHeaders Property," Microsoft Learn, 2025.
// https://learn.microsoft.com/en-us/dotnet/api/azure.storage.blobs.models.blobuploadoptions.httpheaders?view=azure-dotnet
// [Accessed: 04-Sep-2026].
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Net;

namespace CoffeeNChillFunctions.Functions
{
    public class UploadStaffDocument
    {
        private readonly ILogger _logger;

        public UploadStaffDocument(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UploadStaffDocument>();
        }

        [Function("UploadStaffDocument")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "documents/upload")] HttpRequestData req)
        {
            _logger.LogInformation("Staff document upload started.");

            var response = req.CreateResponse();
            try
            {
                //Checks that the request has a Content-Type header before continuing
                if (!req.Headers.TryGetValues("Content-Type", out var contentTypeValues))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("Missing Content-Type header.");
                    return response;
                }

                //Reads the content type header so we can find the boundary value
                var contentType = MediaTypeHeaderValue.Parse(contentTypeValues.First());
                //Gets the boundary value, this tells us where each part of the file starts and ends
                var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;

                if (string.IsNullOrWhiteSpace(boundary))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("Request must be multipart/form-data with a valid boundary.");
                    return response;
                }

                //Reads the uploaded file in sections instead of all at once
                var reader = new MultipartReader(boundary, req.Body);
                MultipartSection? section;
                string? uploadedFileName = null;

                //Loops through each section of the uploaded file until there's nothing left
                while ((section = await reader.ReadNextSectionAsync()) != null)
                {
                    var contentDisposition = ContentDispositionHeaderValue.Parse(section.ContentDisposition);

                    //Only continues if the section is a file not just a text field
                    if (contentDisposition.DispositionType.Equals("form-data") &&
                        !string.IsNullOrEmpty(contentDisposition.FileName.Value))
                    {
                        //Removes folder info and just keepss the file name
                        uploadedFileName = Path.GetFileName(contentDisposition.FileName.Value);

                        //Only allows PDF, PNG and JPEG files so we don't accidentally keep unsafe files
                        var allowedExtensions = new[] { ".pdf", ".png", ".jpg", ".jpeg" };
                        //Sets the correct content type based on file extension so Download reports it properly
                        var extension = Path.GetExtension(uploadedFileName)?.ToLower();

                        if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                        {
                            _logger.LogWarning($"Rejected file '{uploadedFileName}' — disallowed extension '{extension}'.");
                            response.StatusCode = HttpStatusCode.BadRequest;
                            await response.WriteStringAsync($"File type not allowed.");
                            return response;
                        }

                        //Connects to the staff-docs container, creates one if it hasn't been created yet
                        string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";
                        var containerClient = new BlobContainerClient(connectionString, "staff-docs");
                        await containerClient.CreateIfNotExistsAsync();

                        //Uploads the file to blob storage, overwrite true replaces any file with the file name
                        var blobClient = containerClient.GetBlobClient(uploadedFileName);
                        //Works out the correct content type from the file's extension. 
                        //Without this Azure stores every uploaded file as "application/octet-stream" type, 
                        //which affects the downloads as it would put in the wrong file type even though the file is fine.
                        string resolvedContentType = "application/octet-stream";

                        if (extension == ".pdf")
                        {
                            resolvedContentType = "application/pdf";
                        }
                        else if (extension == ".png")
                        {
                            resolvedContentType = "image/png";
                        }
                        else if (extension == ".jpg" || extension == ".jpeg")
                        {
                            resolvedContentType = "image/jpeg";
                        }

                        //BlobHttpHeaders attaches HTTP header values like (Content-Type) to a blob when it's uploaded. We only attach ContentType in the following:
                        var headers = new BlobHttpHeaders
                        {
                            ContentType = resolvedContentType
                        };

                        //BlobUploadOptions collects all the optional settings UploadAsync can accept, including the headers object above.
                        var uploadOptions = new BlobUploadOptions
                        {
                            HttpHeaders = headers
                        };

                        //Uploads the file, putting in the content type settings so it's stored correctly
                        await blobClient.UploadAsync(section.Body, uploadOptions);
                    }
                }

                //If no file was found in the request, tells the user instead of saying it worked.
                if (uploadedFileName == null)
                {
                    _logger.LogWarning("Upload request received with no file attached.");
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("No file found in the request.");
                    return response;
                }

                //Logs and sends back a success message once the file has uploaded
                _logger.LogInformation($"Staff document '{uploadedFileName}' uploaded successfully.");
                response.StatusCode = HttpStatusCode.Created;
                await response.WriteStringAsync($"'{uploadedFileName}' uploaded successfully!");
                return response;
            }
            //Catches any unexpected errors so the function doesn't crash 
            catch (Exception ex)
            {
                _logger.LogError($"Upload failed: {ex.Message}");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync($"Error: {ex.Message}");
                return response;
            }
        }
    }
}
