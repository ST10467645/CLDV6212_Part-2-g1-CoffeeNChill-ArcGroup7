/*
 * Code Attribution:
 * The response DTO pattern for staff document information was based on
 * Microsoft's official Azure Blob Storage documentation.
 *
 * Microsoft Learn (2026e). Introduction to Azure Blob Storage. [Online]
 * Available at: https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blobs-introduction
 * [Accessed 04 September 2026].
 */

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