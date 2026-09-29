using System;

namespace CLDVD6212_POE_Example_AssignmentSubmissionEx.Models
{
    public class StudentQuery
    {
        public string QueryId { get; set; } = string.Empty;

        public string StudentNumber { get; set; } = string.Empty;

        public string StudentName { get; set; } = string.Empty;

        public string ModuleCode { get; set; } = string.Empty;

        public string QuerySubject { get; set; } = string.Empty;

        public string QueryDescription { get; set; } = string.Empty;

        public DateTime QueryTimestamp { get; set; }
    }
}
