/*
 * Code Attribution:
 * The error response DTO pattern was based on Microsoft's official
 * Azure Functions HTTP trigger documentation.
 *
 * Microsoft Learn (2025b). Azure Functions HTTP trigger. [Online]
 * Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger?tabs=python-v2%2Cisolated-process%2Cnodejsv4%2Cfunctionsv2&pivots=programming-language-csharp
 * [Accessed 04 September 2026].
 */

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