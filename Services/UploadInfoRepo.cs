using ImageUpload.Entities;
using MediaService.DbContexts;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace MediaService.Services
{
    public class UploadInfoRepo : IUploadInfoRepo
    {
        private readonly MediaServiceContext _context;

        private readonly ILogger<UploadInfoRepo> _logger;

        public UploadInfoRepo(MediaServiceContext context, ILogger<UploadInfoRepo> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<UploadInfo?> GetUploadByGuidAsync(string guid)
        {
            DateTime now = DateTime.Now;

            try
            {
                _logger.LogInformation("Getting upload by guid {guid}", guid);
                return await _context.UploadInfo.FirstOrDefaultAsync(upload =>
                    upload.AzureStorageId == guid
                );
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error getting upload by guid {guid}", guid);
                throw new Exception($"Error getting upload by {guid}", e);
            }
        }

        public async Task<UploadInfo> CreateUploadAsync(UploadInfo uploadToCreate)
        {
            DateTime now = DateTime.Now;

            if (uploadToCreate == null)
            {
                throw new ArgumentNullException(nameof(uploadToCreate));
            }

            try
            {
                _logger.LogInformation($"Creating upload at {now}");
                var AddAsyncInUploadRepo = await _context.UploadInfo.AddAsync(uploadToCreate);

                if (AddAsyncInUploadRepo.State != EntityState.Added)
                {
                    throw new Exception("Error adding upload to context");
                }
                else
                {
                    var SaveChanges = await _context.SaveChangesAsync();

                    if (SaveChanges == 0)
                    {
                        throw new Exception("Error saving changes to the database");
                    }
                    else
                    {
                        return uploadToCreate;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error at {now} creating upload: {e.Message}");
                throw new Exception("Error creating upload", e);
            }
        }

        public async Task<bool> SaveChangesAsync()
        {
            DateTime now = DateTime.Now;
            try
            {
                _logger.LogInformation($"Saving changes to the database at {now}");
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error at {now} saving changes to the database: {e.Message}");
                throw new Exception("Error saving changes to the database", e);
            }
        }
    }
}
