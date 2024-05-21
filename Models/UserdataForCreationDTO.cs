// namespace ImageUpload.Models
// {
//     public class UserdataForCreationDTO(
//         string excavatorName,
//         string excavatorCompany,
//         string excavatorLocation,
//         string excavatorEmail
//     )
//     {
//         public string ExcavatorName { get; set; } = excavatorName;
//         public string ExcavatorCompany { get; set; } = excavatorCompany;
//         public string ExcavatorLocation { get; set; } = excavatorLocation;

//         public string ExcavatorEmail { get; set; } = excavatorEmail;
//     }
// }

namespace ImageUpload.Models
{
    public class UserdataForCreationDTO
    {
        public IFormFile Photo { get; set; }    
        public string UploaderName { get; set; }
        public string UploaderCompany { get; set; }
        public string UploaderLocation { get; set; } 

        public string UploaderEmail { get; set; } 
    }
}