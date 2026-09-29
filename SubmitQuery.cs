using Azure.Storage.Queues;
using CLDVD6212_POE_Example_AssignmentSubmissionEx.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CLDVD6212_POE_Example_AssignmentSubmissionEx
{
    public class SubmitStudentQuery
    {
        private readonly QueueClient _queueClient;
        public SubmitStudentQuery()
        {
            string connectionString =
            Environment.GetEnvironmentVariable("AzureWebJobsStorage")
            ?? throw new InvalidOperationException(
            "AzureWebJobsStorage is not configured.");
            _queueClient = new QueueClient(
            connectionString,
            "student-query-queue",
            new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.None
            });
            _queueClient.CreateIfNotExists();
        }
        [Function("SubmitStudentQuery")]
        public async Task<HttpResponseData> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "post",
            Route = "studentqueries")]
            HttpRequestData req)
        {
            var response = req.CreateResponse();
            try
            {
                var query =
                await JsonSerializer.DeserializeAsync<StudentQuery>(
                req.Body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (query == null ||
                string.IsNullOrWhiteSpace(query.QueryId) ||
                string.IsNullOrWhiteSpace(query.StudentNumber) ||
                string.IsNullOrWhiteSpace(query.StudentName) ||
                string.IsNullOrWhiteSpace(query.ModuleCode))
                {
                    response.StatusCode =
                    HttpStatusCode.BadRequest;
                    await response.WriteStringAsync(
                    "Invalid student query data.");
                    return response;
                }
                string message =
                JsonSerializer.Serialize(query);
                await _queueClient.SendMessageAsync(message);
                response.StatusCode =
                HttpStatusCode.Accepted;
                await response.WriteAsJsonAsync(new
                {
                    message = "Student query submitted successfully",
                    queryId = query.QueryId,
                    status = "Queued"
                });
                return response;
            }
            catch (Exception ex)
            {
                response.StatusCode =
                HttpStatusCode.InternalServerError;
                await response.WriteStringAsync(
                $"Error: {ex.Message}");
                return response;
            }
        }
    }
}

