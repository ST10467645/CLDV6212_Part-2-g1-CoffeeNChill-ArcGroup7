using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;
using CLDVD6212_POE_Example_AssignmentSubmissionEx.Models;

namespace CLDVD6212_POE_Example_AssignmentSubmissionEx
{
    public class UpdateAssignment
    {
        [Function("UpdateAssignment")]
        public async Task<HttpResponseData> Run(
        [HttpTrigger(
AuthorizationLevel.Anonymous,
"put",
Route = "assignments/{partitionKey}/{rowKey}"
)] HttpRequestData req,
        string partitionKey,
        string rowKey)
        {
            var response = req.CreateResponse();
            try
            {
                string body =
                await new StreamReader(req.Body).ReadToEndAsync();
                var assignment =
                JsonSerializer.Deserialize<Assignment>(
                body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (assignment == null)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync(
                    "Invalid JSON.");
                    return response;
                }
                string connectionString =
                "UseDevelopmentStorage=true";
                TableClient table =
                new TableClient(
                connectionString,
                "AssignmentSubmissios");
                assignment.PartitionKey = partitionKey;
                assignment.RowKey = rowKey;
                var existing =
                await table.GetEntityAsync<Assignment>(
                partitionKey,
                rowKey);
                assignment.ETag = existing.Value.ETag;
                await table.UpdateEntityAsync(
                assignment,
                assignment.ETag,
                TableUpdateMode.Replace);
                response.StatusCode = HttpStatusCode.OK;
                await response.WriteStringAsync(
                "Assignment updated successfully!");
                return response;
            }
            catch (RequestFailedException ex)
            {
                response.StatusCode =
                (HttpStatusCode)ex.Status;
                await response.WriteStringAsync(
                $"Azure Table Error\n" +
                $"Status: {ex.Status}\n" +
                $"Error Code: {ex.ErrorCode}\n" +
                $"Message: {ex.Message}");
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