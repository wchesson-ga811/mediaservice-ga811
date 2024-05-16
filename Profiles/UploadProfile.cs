using AutoMapper;

namespace ImageUpload.Profiles
{
    public class UploadProfile : Profile
    {
        public UploadProfile()
        {
            CreateMap<Entities.UploadInfo, Models.UploadDTO>();
            CreateMap<Models.UploadForCreationDTO, Entities.UploadInfo>();
        }
    }
}