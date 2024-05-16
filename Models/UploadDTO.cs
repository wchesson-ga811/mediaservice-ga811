namespace ImageUpload.Models
{
    public class UploadDTO(
        string excavatorName,
        string excavatorCompany,
        string excavatorLocation,
        string excavatorEmail
    )
    {
        public string AzureStorageId { get; set; }
        public string ExcavatorName { get; set; } = excavatorName;
        public string ExcavatorCompany { get; set; } = excavatorCompany;
        public string ExcavatorLocation { get; set; } = excavatorLocation;

        public string ExcavatorEmail { get; set; } = excavatorEmail;

        public Dictionary<string, string> Metadata { get; set; }

        public DateTime UploadTime { get; set; } = DateTime.Now;
    }
}
