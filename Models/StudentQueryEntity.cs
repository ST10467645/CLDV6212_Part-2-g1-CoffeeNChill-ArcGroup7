using Azure;
using Azure.Data.Tables;
using System;

namespace CLDVD6212_POE_Example_AssignmentSubmissionEx.Models
{
    public class StudentQueryEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;

        public string RowKey { get; set; } = string.Empty;

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }

        public string QueryId { get; set; } = string.Empty;

        public string StudentNumber { get; set; } = string.Empty;

        public string StudentName { get; set; } = string.Empty;

        public string ModuleCode { get; set; } = string.Empty;

        public string QuerySubject { get; set; } = string.Empty;

        public string QueryDescription { get; set; } = string.Empty;

        public string QueryTimestamp { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }
}
