// Attribution: response DTO pattern based on the Azure Blob Storage introduction (Microsoft Learn, 2026e).
namespace CoffeeNChillFunctions.DTOs
{
    // Defines the DTO that is returned for every staff document in the list: its name, its size and when it was last modified.
    public class StaffDocumentResponse
    {
        public string FileName { get; set; } = string.Empty;
        public long? SizeInBytes { get; set; }
        public DateTimeOffset? LastModified { get; set; }
    }
}