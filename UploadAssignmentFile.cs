using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;


namespace CLDVD6212_POE_Example_AssignmentSubmissionEx;

public class UploadAssignmentFile
{
    private readonly ILogger _logger;

    public UploadAssignmentFile(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<UploadAssignmentFile>();
    }

    [Function("UploadAssignmentFile")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequestData req)
    {
        _logger.LogInformation("Assignment file upload started.");

        string? fileName =
            System.Web.HttpUtility.ParseQueryString(req.Url.Query)["fileName"];

        if (string.IsNullOrEmpty(fileName))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);

            await badResponse.WriteStringAsync("Please provide a fileName in the URL.");

            return badResponse;
        }

        string? studentNumber = System.Web.HttpUtility.ParseQueryString(req.Url.Query)["studentNumber"];

        if (string.IsNullOrEmpty(studentNumber))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);

            await badResponse.WriteStringAsync("Please provide a studentNumber in the URL.");

            return badResponse;
        }

        if (req.Body == null)
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);

            await badResponse.WriteStringAsync("No file was provided.");

            return badResponse;
        }

        // Connect to azurite
        string connectionstring = "UseDevelopmentStorage=true";

        BlobServiceClient blobServiceClient = new BlobServiceClient(connectionstring);

        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("assignments");

        await containerClient.CreateIfNotExistsAsync();

        string blobName = $"{studentNumber} / {fileName}";

        BlobClient blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(req.Body, overwrite: true);

        _logger.LogInformation($"Assignment file uploaded: {blobName}");

        var response = req.CreateResponse(HttpStatusCode.OK);

        await response.WriteStringAsync($"Assignment file uploaded successfully: {blobName}");

        return response;
    }
}