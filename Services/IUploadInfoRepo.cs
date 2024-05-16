using ImageUpload.Entities;

namespace MediaService.Services
{
    public interface IUploadInfoRepo
    {
        Task<UploadInfo?> GetUploadByGuidAsync(string guid);

        Task<UploadInfo> CreateUploadAsync(UploadInfo upload);

        Task<bool> SaveChangesAsync();
    }
}