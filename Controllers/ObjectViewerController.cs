using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;

namespace ObjectViewer.Controllers
{
    [ApiController]
    [Route("api/objectviewer")]
    public class ObjectViewerController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ObjectViewerController> _logger;

        public ObjectViewerController(
            IConfiguration configuration,
            ILogger<ObjectViewerController> logger
        )
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Object Viewer API");
        }

        [HttpPost("check-for-photo")]
        public async Task<IActionResult> CheckForPhoto([FromBody] string guid)
        {
            try
            {
                _logger.LogInformation(
                    $"Received backend request to check for photo with GUID {guid}"
                );

                // Retrieve the connection string for the Azure Blob Storage
                string connectionString =
                    _configuration.GetConnectionString("ConnectionStrings:AzureMediaService")
                    ?? string.Empty;

                var blobServiceClient = new BlobServiceClient(
                    new Uri("https://mediaservicega811.blob.core.windows.net/"),
                    new DefaultAzureCredential()
                );

                var containerClient = blobServiceClient.GetBlobContainerClient("images");

                BlobClient blobClient = containerClient.GetBlobClient(guid);

                if (await blobClient.ExistsAsync())
                {
                    _logger.LogInformation(
                        $"Photo with GUID {guid} found. Retrieving photo from blob storage."
                    );

                    var fileBytes = await DownloadBlobFromStreamAsync(blobClient);

                    string base64FileContent = Convert.ToBase64String(fileBytes);
                    return Ok(
                        new
                        {
                            fileContent = base64FileContent,
                            statusCode = 200,
                            guid = guid
                        }
                    );
                }
                else
                {
                    _logger.LogInformation($"Photo with GUID {guid} not found");

                    return NotFound("Photo not found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());

                var errorResponse = new { message = ex.StackTrace, exception = ex.ToString() };

                return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
            }
        }

        private static async Task<byte[]> DownloadBlobFromStreamAsync(BlobClient blobClient)
        {
            using (var stream = await blobClient.OpenReadAsync())
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }
    }
}
