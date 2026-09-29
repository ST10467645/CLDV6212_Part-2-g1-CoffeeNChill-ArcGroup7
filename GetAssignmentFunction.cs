using Microsoft.Azure.Functions.Worker;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker.Http;
using CLDVD6212_POE_Example_AssignmentSubmissionEx.Models;
using System.Net;

namespace CLDVD6212_POE_Example_AssignmentSubmissionEx;

public class GetAssignmentFunction
{
    [Function("GetAssignmentFunction")]

    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)

    {

        string connectionstring = "UseDevelopmentStorage=true";

        // Make sure table name is the original one that had a spelling mistake (AssignmentSubmissios") so it doenst keep making new table (ONLY FOR THIS POE EXAMPLE NOT IN ACTUAL POE)
        TableClient table = new TableClient(connectionstring, "AssignmentSubmissios");

        await table.CreateIfNotExistsAsync();

        var assignments = table.Query<Assignment>().ToList();

        var response = req.CreateResponse(HttpStatusCode.OK);

        await response.WriteAsJsonAsync(assignments);

        return response;

    }

}