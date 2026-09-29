using Azure.Data.Tables;
using CLDVD6212_POE_Example_AssignmentSubmissionEx.Models;
using Microsoft.Azure.Functions.Worker;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace CLDVD6212_POE_Example_AssignmentSubmissionEx
{
    public class ProcessStudentQuery
    {
        private readonly TableClient _tableClient;

        public ProcessStudentQuery()
        {
            string connectionString =
                Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                ?? throw new InvalidOperationException(
                    "AzureWebJobsStorage is not configured.");

            _tableClient = new TableClient(
                connectionString,
                "StudentQueries");
        }

        [Function("ProccessStudentQuery")]
        public async Task Run(
            [QueueTrigger(
                "student-query-queue",
                Connection = "AzureWebJobsStorage")]
            string queueMessage)
        {
            Console.WriteLine("=============================");
            Console.WriteLine("STUDENT QUERY RECIVED");
            Console.WriteLine("=============================");

            Console.WriteLine(queueMessage);

            var query = JsonSerializer.Deserialize<StudentQuery>(
                queueMessage,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (query == null)
            {
                throw new InvalidOperationException(
                    "Unable to deserialize query.");
            }

            var entity = new StudentQueryEntity
            {
                PartitionKey = query.ModuleCode,
                RowKey = query.QueryId,

                QueryId = query.QueryId,
                StudentNumber = query.StudentNumber,
                StudentName = query.StudentName,
                ModuleCode = query.ModuleCode,
                QuerySubject = query.QuerySubject,
                QueryDescription = query.QueryDescription,

                QueryTimestamp = query.QueryTimestamp.ToString("o"),

                Status = "Recieved"
            };

            await _tableClient.CreateIfNotExistsAsync();

            await _tableClient.UpsertEntityAsync(entity);

            Console.WriteLine("=============================");
            Console.WriteLine("STUDENT QUERY SAVED");
            Console.WriteLine("=============================");

            Console.WriteLine($"Query ID: {query.QueryId}");
            Console.WriteLine($"Student: {query.StudentName}");
            Console.WriteLine($"Status: {entity.Status}");

            Console.WriteLine("=============================");
        }
    }
}
