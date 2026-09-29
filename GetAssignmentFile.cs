using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Web;

namespace CLDVD6212_POE_Example_AssignmentSubmissionEx;

public class GetAssignmentFile
{
    private readonly ILogger<GetAssignmentFile> _logger;

    public GetAssignmentFile(ILogger<GetAssignmentFile> logger)
    {
        _logger = logger;
    }

    [Function("GetAssignmentFile")]
    public async Task<IActionResult> Run(
      [HttpTrigger(AuthorizationLevel.Anonymous, "get")]
    HttpRequest req)
    {
        // Read query parameters
        string? studentNumber = req.Query["studentNumber"];
        string? fileName = req.Query["fileName"];

        // Validate parameters
        if (string.IsNullOrWhiteSpace(studentNumber) ||
      string.IsNullOrWhiteSpace(fileName))
        {
            return new BadRequestObjectResult(
              "Please provide studentNumber and fileName.");
        }

        // Azure Storage connection string
        // For local Azurite development
        string connectionString = "UseDevelopmentStorage=true";

        try
        {
            // Create Blob service client
            var blobServiceClient =
        new BlobServiceClient(connectionString);

            // Get assignments container
            BlobContainerClient containerClient =
        blobServiceClient.GetBlobContainerClient("assignments");

            // Blob path:
            // assignments/{studentNumber}/{fileName}
            string blobName = $"{studentNumber} / {fileName}";

            BlobClient blobClient =
              containerClient.GetBlobClient(blobName);

            _logger.LogInformation(
              "Looking for assignment file: {BlobName}",
              blobName);

            // Check if the file exists
            if (!await blobClient.ExistsAsync())
            {
                return new NotFoundObjectResult(
                  "Assignment file was not found.");
            }

            // Download blob
            var download =
        await blobClient.DownloadStreamingAsync();

            // Determine the file's content type
            string contentType = fileName.ToLowerInvariant() switch
            {
                var f when f.EndsWith(".pdf") =>
                  "application/pdf",

                var f when f.EndsWith(".jpg") ||
                      f.EndsWith(".jpeg") =>
                  "image/jpeg",

                var f when f.EndsWith(".png") =>
                  "image/png",

                var f when f.EndsWith(".txt") =>
                  "text/plain",

                var f when f.EndsWith(".docx") =>
                  "application/vnd.openxmlformats-officedocument.wordprocessingml.document",

                var f when f.EndsWith(".xlsx") =>
                  "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                _ => "application/octet-stream"
            };

            _logger.LogInformation(
              "Returning file {FileName} with content type {ContentType}",
              fileName,
              contentType);

            // Return the file to the browser/client
            return new FileStreamResult(
        download.Value.Content,
        contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(
              ex,
              "Error downloading assignment file.");

            return new StatusCodeResult(
              StatusCodes.Status500InternalServerError);
        }
    }
}