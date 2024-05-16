using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace ImageUpload.Entities
{
    public class UploadInfo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UploadId { get; set; }

        [Required]
        public string AzureStorageId { get; set; }

        [Required]
        public string ExcavatorName { get; set; }

        [Required]
        public string ExcavatorCompany { get; set; }

        [Required]
        public string ExcavatorEmail { get; set; }

        [Required]
        public string ExcavatorLocation { get; set; }

        [NotMapped]
        public Dictionary<string, string> Metadata { get; set; }

        [Required]
        public string MetadataJson
        {
            get => JsonConvert.SerializeObject(Metadata);
            set => Metadata = JsonConvert.DeserializeObject<Dictionary<string, string>>(value);
        }

        [Required]
        public DateTime UploadTime { get; set; }
    }
}
