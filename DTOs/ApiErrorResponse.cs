// Attribution:
// error response DTO pattern based on the Azure Functions HTTP trigger documentation (Microsoft Learn, 2025b).
using System.Text.Json.Serialization;

namespace CoffeeNChillFunctions.DTOs
{
    // Defines the DTO that is returned for a single error, for example a 404 Not Found or a 503 Service Unavailable.
    public class ErrorResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;

        public ErrorResponse() { }

        public ErrorResponse(string error)
        {
            Error = error;
        }
    }

    // Defines the DTO that is returned when validation fails, so that every problem is listed together in one 400 Bad Request.
    public class ValidationErrorResponse
    {
        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new();

        public ValidationErrorResponse() { }

        public ValidationErrorResponse(string error)
        {
            Errors = new List<string> { error };
        }

        public ValidationErrorResponse(List<string> errors)
        {
            Errors = errors;
        }
    }
}