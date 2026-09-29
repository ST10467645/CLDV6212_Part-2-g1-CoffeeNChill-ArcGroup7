using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace CLDVD6212_POE_Example_AssignmentSubmissionEx
{
    public class DeleteAssignment
    {
        [Function("DeleteAssignment")]
        public async Task<HttpResponseData> Run(
        [HttpTrigger(
    AuthorizationLevel.Anonymous,
    "delete",
    Route = "assignments/{partitionKey}/{rowKey}"
    )] HttpRequestData req,
            string partitionKey,
            string rowKey)
            {
                var response = req.CreateResponse();
                try
                {
                    string connectionString =
                    "UseDevelopmentStorage=true";
                    TableClient table =
                    new TableClient(
                    connectionString,
                    "AssignmentSubmissios");
                    await table.DeleteEntityAsync(
                    partitionKey,
                    rowKey);
                    response.StatusCode = HttpStatusCode.OK;
                    await response.WriteStringAsync(
                    "Assignment deleted successfully!");
                    return response;
                }
                catch (RequestFailedException ex)
                {
                    response.StatusCode =
                    (HttpStatusCode)ex.Status;
                    await response.WriteStringAsync(
                    $"Azure Table Error: {ex.ErrorCode} - {ex.Message}");
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