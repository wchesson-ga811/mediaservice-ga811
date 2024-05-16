using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ImageUpload.Entities;
using ImageUpload.Models;
using MediaService.Services;
using MetadataExtractor;
using MetadataExtractor.Formats.Tiff;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using nClam;

namespace PhotoUpload.Controllers
{
    [Route("api/photoupload")]
    [ApiController]
    public class PhotoUploadController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PhotoUploadController> _logger;

        private readonly IMapper _mapper;

        ClamClient clam = new ClamClient("localhost", 3310);

        private readonly IUploadInfoRepo _uploadInfoRepo;

        public PhotoUploadController(
            // IConfiguration configuration,
            ILogger<PhotoUploadController> logger,
            IMapper mapper,
            IUploadInfoRepo uploadInfoRepo
        )
        {
            // _configuration = configuration;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _uploadInfoRepo =
                uploadInfoRepo ?? throw new ArgumentNullException(nameof(uploadInfoRepo));
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Photo upload controller is up and running");
        }

        [HttpPost]
        public async Task<IActionResult> UploadPhoto(IFormFile photo)
        {
            DateTime now = DateTime.Now;

            try
            {
                _logger.LogInformation($"Received backend request to upload photo at {now}");

                // _logger.LogInformation("Scanning for malware");

                if (photo == null || photo.Length == 0)
                {
                    return BadRequest("No photo found");
                }

                // IActionResult isFileClean = await ScanPhotoWithClamAV(photo);

                // if (isFileClean is OkObjectResult)
                // {
                // Retrieve the connection string for the Azure Blob Storage
                string connectionString =
                    _configuration.GetConnectionString("ConnectionStrings:AzureMediaService")
                    ?? string.Empty;

                var blobServiceClient = new BlobServiceClient(
                    new Uri("https://mediaservicega811.blob.core.windows.net/"),
                    new DefaultAzureCredential()
                );

                var containerClient = blobServiceClient.GetBlobContainerClient("images");

                //save original file name
                string originalFileName = photo.FileName;
                Console.WriteLine($"Original file name: {originalFileName}");

                // Create a unique name for the photo
                string guidFileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);

                await using var stream = photo.OpenReadStream();
                var uploaded = await containerClient.UploadBlobAsync(guidFileName, stream);

                BlobClient blobClient;

                if (uploaded != null)
                {
                    blobClient = containerClient.GetBlobClient(guidFileName);
                    Console.WriteLine($"Uploaded photo to {blobClient.Uri} at {now}");
                }
                else
                {
                    return BadRequest("Backend: failed to upload photo");
                }

                var exifMetadata = await ExtractExifMetadata(photo);

                exifMetadata.Add("GUIDFileName", guidFileName);

                return Ok(exifMetadata);
                // }
                // else if (isFileClean is UnauthorizedObjectResult)
                // {
                //     return BadRequest(
                //         "There was malware detected in your photo. Please submit another photo to complete your request."
                //     );
                // }
                // else
                // {
                //     return BadRequest(
                //         "There was an error scanning your photo for malware. Please try another photo or at a later time."
                //     );
                // }

                //error handling
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());

                var errorResponse = new { message = ex.StackTrace, exception = ex.ToString() };

                return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
            }
        }

        [HttpPost("extract-exif-metadata")]
        public async Task<Dictionary<string, string>> ExtractExifMetadata(IFormFile photo)
        {
            // return Ok();
            _logger.LogInformation("Extracting EXIF metadata from photo");

            using (var stream = photo.OpenReadStream())
            {
                // Ensure the stream is positioned at the beginning of the file
                stream.Position = 0;

                // Read metadata from the stream
                IEnumerable<MetadataExtractor.Directory> directories =
                    ImageMetadataReader.ReadMetadata(stream);

                // directories
                //     .SelectMany(directory => directory.Tags)
                //     .ToList()
                //     .ForEach(tag =>
                //     {
                //         string snakeCaseName = tag.Name.Replace(" ", ""); // Trims white spaces and other whitespace characters

                //         // _logger.LogInformation($"{snakeCaseName}");
                //     });

                // Extract EXIF tags and values
                // var exifMetadata = directories
                //     .SelectMany(directory => directory.Tags)

                var exifMetadata = directories
                    .SelectMany(directory => directory.Tags)
                    .GroupBy(tag => tag.Name.Replace(" ", "")) // Group tags by name
                    .ToDictionary(
                        group => group.Key, // Use the tag name as the dictionary key
                        group => string.Join("; ", group.Select(tag => tag.Description)) // Concatenate descriptions
                    );

                _logger.LogInformation("EXIF metadata extracted successfully");

                return exifMetadata;
            }
        }

        // [HttpPost("store-sender-data")]
        // public async Task<IActionResult> StoreSenderData(
        //     // string excavatorName,
        //     // string excavatorCompany,
        //     // string excavatorLocation
        //     UserData userData
        // )
        // {
        //     DateTime now = DateTime.Now;

        //     try
        //     {
        //         _logger.LogInformation($"Received backend request to store sender data at {now}");

        //         Dictionary<string, string> senderData = new Dictionary<string, string>
        //         {
        //             { "ExcavatorName", userData.ExcavatorName },
        //             { "ExcavatorCompany", userData.ExcavatorCompany },
        //             { "ExcavatorLocation", userData.ExcavatorLocation }
        //         };

        //         return Ok(senderData);
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        //     }
        // }

        [HttpPost("store-upload-data")]
        public async Task<ActionResult<UploadForCreationDTO>> StoreUploadData(
            UploadForCreationDTO uploadDTO,
            string guid
        )
        {
            DateTime now = DateTime.Now;

            try
            {
                _logger.LogInformation("Mapping UploadForCreationDTO to entity");

                var uploadEntity = _mapper.Map<UploadInfo>(uploadDTO);

                _logger.LogInformation("Creating new upload entity");
                await _uploadInfoRepo.CreateUploadAsync(uploadEntity);

                return CreatedAtAction(
                    "UploadPhoto",
                    new { guid = uploadEntity.AzureStorageId },
                    _mapper.Map<UploadForCreationDTO>(uploadEntity)
                );
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("scan-file")]
        public async Task<IActionResult> ScanPhotoWithClamAV(IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
                return Content("file not selected");

            var ms = new MemoryStream();
            photo.OpenReadStream().CopyTo(ms);
            byte[] fileBytes = ms.ToArray();

            _logger.LogInformation("ClamAV received request to scan {0}", photo.FileName);
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
                    Console.WriteLine("The file is clean!");
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

            _logger.LogInformation("ClamAV scan completed for file {0}", photo.FileName);

            // Add a return statement at the end of the method
            return BadRequest("Could not scan the file.");
        }
    }
}
