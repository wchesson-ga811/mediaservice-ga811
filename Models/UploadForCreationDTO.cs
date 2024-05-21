// namespace ImageUpload.Models {
//     public class UploadForCreationDTO(string excavatorName, string excavatorCompany, string excavatorLocation, string excavatorEmail, Dictionary<string, string> metadata, string azureStorageId) {

//         //frontend values
//         public string ExcavatorName { get; set; } = excavatorName;
//         public string ExcavatorCompany { get; set; } = excavatorCompany;
//         public string ExcavatorLocation { get; set; } = excavatorLocation;
//         public string ExcavatorEmail { get; set; } = excavatorEmail;

//         //backend values
//         public string AzureStorageId { get; set; } = azureStorageId;
//         public Dictionary<string, string> Metadata { get; set; } = metadata;

//         public DateTime UploadTime { get; set; } = DateTime.Now;
//     }
// }


namespace ImageUpload.Models
{
    public class UploadForCreationDTO
    {
        //frontend values
        public string UploaderName { get; set; }
        public string UploaderCompany { get; set; }
        public string UploaderLocation { get; set; }
        public string UploaderEmail { get; set; }

        //backend values
        public string AzureStorageId { get; set; }
        public Dictionary<string, string> Metadata { get; set; }

        public DateTime UploadTime { get; set; } = DateTime.Now;
    }
}
