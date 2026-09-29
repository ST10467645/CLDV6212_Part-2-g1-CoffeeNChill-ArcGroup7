using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using CLDVD6212_POE_Example_AssignmentSubmissionEx.Models;
using Microsoft.Azure.Functions.Worker.Http;
using Azure.Data.Tables;

namespace CLDVD6212_POE_Example_AssignmentSubmissionEx;

public class Function1
{
    private readonly ILogger _logger;

    public Function1(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Function1>();
    }

    [Function("SubmitAssignment")]
    public async Task<HttpResponseData> Run(

        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]

        HttpRequestData req)
    {
        _logger.LogInformation("Assignment recieved.");

        // Read JSON from Postman
        string body = await new StreamReader(req.Body).ReadToEndAsync();

        var assignment = JsonSerializer.Deserialize<Assignment>(
            body,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

        _logger.LogInformation($"BODY = {body}");

        if (assignment != null)
        {
            _logger.LogInformation($"StudentNumber = {assignment.StudentNumber}");
            _logger.LogInformation($"StudentName = {assignment.StudentName}");
            _logger.LogInformation($"ModuleCode = {assignment.ModuleCode}");
            _logger.LogInformation($"AssignmentName = {assignment.AssignmentName}");
        }

        if (assignment == null)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Invalid JSON");
            return bad;
        }

        // Azure Table Storage requires these Keys
        assignment.PartitionKey = "Assignments";
        assignment.RowKey = Guid.NewGuid().ToString();

        // Connect to Azurite
        string connectionString = "UseDevelopmentStorage=true";

        // Connect to the table
        TableClient table = new TableClient(connectionString, "AssignmentSubmissios");

        // Create the table if it doesnt exist
        await table.CreateIfNotExistsAsync();

        // Save the assignment
        await table.AddEntityAsync(assignment);

        _logger.LogInformation("Assignment saved to Azure Table Storage");
        _logger.LogInformation($"StudentNumber = {assignment.StudentNumber}");

        var response = req.CreateResponse(HttpStatusCode.OK);

        await response.WriteStringAsync("Assignment saved successfully!");

        return response;


    }
}