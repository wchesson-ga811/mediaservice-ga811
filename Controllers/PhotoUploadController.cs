using System.Diagnostics.CodeAnalysis;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BlobTutorial_V2.Models;
using MetadataExtractor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using nClam;

namespace BlobTutorial_V2.Controllers
{
    [Route("api/photoupload")]
    [ApiController]
    public class PhotoUploadController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PhotoUploadController> _logger;

        ClamClient clam = new ClamClient("localhost", 3310);

        public PhotoUploadController(
            IConfiguration configuration,
            ILogger<PhotoUploadController> logger
        )
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> UploadPhoto(IFormFile photo)
        {
            DateTime now = DateTime.Now;

            try
            {
                _logger.LogInformation($"Received backend request to upload photo at {now}");

                _logger.LogInformation("Scanning for malware");

                if (photo == null || photo.Length == 0)
                {
                    return BadRequest("No photo found");
                }

                IActionResult isFileClean = await ScanPhotoWithClamAV(photo);

                if (isFileClean is OkObjectResult)
                {
                    // Retrieve the connection string for the Azure Blob Storage
                    string connectionString =
                        _configuration.GetConnectionString("ConnectionStrings:AzureMediaService")
                        ?? string.Empty;

                    var blobServiceClient = new BlobServiceClient(
                        new Uri("https://mediaservicega811.blob.core.windows.net/"),
                        new DefaultAzureCredential()
                    );

                    var containerClient = blobServiceClient.GetBlobContainerClient("images");

                    // Create a unique name for the photo
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);

                    await using var stream = photo.OpenReadStream();
                    var uploaded = await containerClient.UploadBlobAsync(photo.FileName, stream);

                    BlobClient blobClient;

                    if (uploaded != null)
                    {
                        blobClient = containerClient.GetBlobClient(fileName);
                        Console.WriteLine($"Uploaded photo to {blobClient.Uri} at {now}");
                    }
                    else
                    {
                        return BadRequest("Backend: failed to upload photo");
                    }

                    var exifMetadata = ExtractExifMetadata(photo);

                    exifMetadata.Add("Uri", blobClient.Uri.ToString());

                    return Ok(exifMetadata);
                }
                else if (isFileClean is UnauthorizedObjectResult)
                {
                    return BadRequest(
                        "There was malware detected in your photo. Please submit another photo to complete your request."
                    );
                }
                else
                {
                    return BadRequest(
                        "There was an error scanning your photo for malware. Please try another photo or at a later time."
                    );
                }

                //error handling
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());

                var errorResponse = new { message = ex.Message, exception = ex.ToString() };

                return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
            }
        }

        private Dictionary<string, string> ExtractExifMetadata(IFormFile photo)
        {
            using (var stream = photo.OpenReadStream())
            {
                // Ensure the stream is positioned at the beginning of the file
                stream.Position = 0;

                // Read metadata from the stream
                IEnumerable<MetadataExtractor.Directory> directories =
                    ImageMetadataReader.ReadMetadata(stream);

                // Extract EXIF tags and values
                var exifMetadata = directories
                    .SelectMany(directory => directory.Tags)
                    .ToDictionary(tag => tag.Name, tag => tag.Description);

                return exifMetadata;
            }
        }

        [HttpPost("store-sender-data")]
        public async Task<IActionResult> StoreSenderData(
            // string excavatorName,
            // string excavatorCompany,
            // string excavatorLocation
            UserData userData
        )
        {
            DateTime now = DateTime.Now;

            try
            {
                _logger.LogInformation($"Received backend request to store sender data at {now}");

                Dictionary<string, string> senderData = new Dictionary<string, string>
                {
                    { "ExcavatorName", userData.ExcavatorName },
                    { "ExcavatorCompany", userData.ExcavatorCompany },
                    { "ExcavatorLocation", userData.ExcavatorLocation }
                };

                return Ok(senderData);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // [HttpPost("scan-file")]
        private async Task<IActionResult> ScanPhotoWithClamAV(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Content("file not selected");

            var ms = new MemoryStream();
            file.OpenReadStream().CopyTo(ms);
            byte[] fileBytes = ms.ToArray();

            _logger.LogInformation("ClamAV received request to scan {0}", file.FileName);
            // var clam = new ClamClient("localhost", 3310);

            var scanResult = await clam.SendAndScanFileAsync(fileBytes);
            _logger.LogInformation("Scan result: {0}", scanResult.Result);

            switch (scanResult.Result)
            {
                case ClamScanResults.Clean:
                    _logger.LogInformation(
                        "The file is clean! ScanResult:{1}",
                        scanResult.RawResult
                    );
                    return Ok(true);

                case ClamScanResults.VirusDetected:
                    if (scanResult.InfectedFiles != null && scanResult.InfectedFiles.Any())
                    {
                        _logger.LogError(
                            "Virus Found! Virus name: {1}",
                            scanResult.InfectedFiles.FirstOrDefault()?.VirusName
                        );
                    }
                    else
                    {
                        _logger.LogError("Virus Found! But no virus name available.");
                    }
                    return Unauthorized(false);
                case ClamScanResults.Error:
                    _logger.LogError(
                        "An error occured while scanning the file! ScanResult: {1}",
                        scanResult.RawResult
                    );
                    return BadRequest("An error occured while scaning the file!");

                case ClamScanResults.Unknown:
                    _logger.LogError(
                        "Unknown scan result while scanning the file! ScanResult: {0}",
                        scanResult.RawResult
                    );

                    return BadRequest("Unknown scan result while scaning the file!");
            }

            _logger.LogInformation("ClamAV scan completed for file {0}", file.FileName);

            // Add a return statement at the end of the method
            return BadRequest("Could not scan the file.");
        }
    }
}
