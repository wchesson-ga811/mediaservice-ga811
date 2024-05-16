
namespace ImageUpload.Models {
    public class UploadForCreationDTO(string excavatorName, string excavatorCompany, string excavatorLocation, string excavatorEmail, Dictionary<string, string> metadata, string azureStorageId) {
        // public string AzureStorageId { get; set; } = azureStorageId;
        public string ExcavatorName { get; set; } = excavatorName;
        public string ExcavatorCompany { get; set; } = excavatorCompany;
        public string ExcavatorLocation { get; set; } = excavatorLocation;
        public string ExcavatorEmail { get; set; } = excavatorEmail;
        // public Dictionary<string, string> Metadata { get; set; } = metadata;
        // public DateTime UploadTime { get; set; } = DateTime.Now;
    }
}