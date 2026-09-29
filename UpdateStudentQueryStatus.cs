using Azure.Data.Tables;
using CLDVD6212_POE_Example_AssignmentSubmissionEx.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace CLDVD6212_POE_Example_AssignmentSubmissionEx
{
    public class UpdateStudentQueryStatus
    {
        private readonly TableClient _tableClient;
        public UpdateStudentQueryStatus()
        {
            string connectionString =
            Environment.GetEnvironmentVariable("AzureWebJobsStorage")
            ?? throw new InvalidOperationException(
            "AzureWebJobsStorage is not configured.");
            _tableClient = new TableClient(
            connectionString,
            "StudentQueries");
        }
        [Function("UpdateStudentQueryStatus")]
        public async Task<HttpResponseData> Run(
        [HttpTrigger(
        AuthorizationLevel.Anonymous,
        "put",
        Route = "studentqueries/{moduleCode}/{queryId}/status")]
        HttpRequestData req,
        string moduleCode,
        string queryId)
        {
            var response = req.CreateResponse();
            try
            {
                var ticket =
                _tableClient.GetEntity<StudentQueryEntity>(
                moduleCode,
                queryId);
                string newStatus =
                await new StreamReader(req.Body).ReadToEndAsync();
                newStatus = newStatus.Trim();
                if (newStatus != "Assigned" &&
                newStatus != "In Progress" &&
                newStatus != "Resolved")
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync(
                    "Invalid status. Use Assigned, In Progress, or Resolved.");
                    return response;
                }
                ticket.Value.Status = newStatus;
                await _tableClient.UpdateEntityAsync(
                ticket.Value,
                ticket.Value.ETag);
                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(new
                {
                    message = "Student query status updated successfully",
                    queryId = queryId,
                    status = newStatus
                });
                return response;
            }
            catch (Azure.RequestFailedException ex)
            when (ex.Status == 404)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteStringAsync(
                "Student query not found.");
                return response;
            }
            catch (Exception ex)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync(
                $"Error: {ex.Message}");
                return response;
            }
        }
    }
}

