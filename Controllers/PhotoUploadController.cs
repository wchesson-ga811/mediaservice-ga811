using AutoMapper;
using Azure.Identity;
using Azure.Storage.Blobs;
using ImageUpload.Entities;
using ImageUpload.Models;
using MediaService.Services;
using MetadataExtractor;
using Microsoft.AspNetCore.Mvc;
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

        ClamClient clam = new ClamClient("20.127.158.138", 3310);

        private readonly IUploadInfoRepo _uploadInfoRepo;

        public PhotoUploadController(
            IConfiguration configuration,
            ILogger<PhotoUploadController> logger,
            IMapper mapper,
            IUploadInfoRepo uploadInfoRepo
        )
        {
            _configuration = configuration;
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
        public async Task<IActionResult> UploadPhoto([FromForm] UserdataForCreationDTO userData)
        {
            DateTime now = DateTime.Now;

            // var photo = Request.Form.Files[0];
            var photo = userData.Photo;

            try
            {
                _logger.LogInformation($"Received backend request to upload photo at {now}");

                if (photo == null || photo.Length == 0)
                {
                    return BadRequest("No photo found");
                }
                else if (userData == null)
                {
                    return BadRequest("No user data found");
                }

                _logger.LogInformation("Scanning for malware");
                // IActionResult isFileClean = await ScanPhotoWithClamAV(photo);

                // if (isFileClean is OkObjectResult)
                // {
                    // Create a unique name for the photo
                    string guidFileName =
                        Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);

                    var uploadToBlob = await UploadBlob(photo, guidFileName);

                    Dictionary<string, string> exifMetadata;
                    if (uploadToBlob is OkObjectResult)
                    {
                        exifMetadata = await ExtractExifMetadata(photo);

                        //set values to for upload entity
                        var uploadForCreationDTO = new UploadForCreationDTO
                        {
                            AzureStorageId = guidFileName,
                            Metadata = exifMetadata,
                            UploadTime = now,
                            UploaderCompany = userData.UploaderCompany,
                            UploaderEmail = userData.UploaderEmail,
                            UploaderLocation = userData.UploaderLocation,
                            UploaderName = userData.UploaderName,
                        };

                        var dataUploaded = await StoreUploadData(uploadForCreationDTO);

                        // ActionResult dataUploaded = await StoreUploadData(uploadForCreationDTO);

                        if (dataUploaded.Result is BadRequestObjectResult)
                        {
                            return BadRequest("Failed to store upload data");
                        }
                        else if (dataUploaded.Result is CreatedAtActionResult)
                        {
                            return CreatedAtAction(
                                "UploadPhoto",
                                new { guid = guidFileName },
                                _mapper.Map<UploadForCreationDTO>(uploadForCreationDTO)
                            );
                        }
                    }
                    // exifMetadata.Add("GUIDFileName", guidFileName);

                    // return Ok(exifMetadata);
                    // }
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

                return Ok("Photo uploaded successfully");

                //error handling
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());

                var errorResponse = new { message = ex.StackTrace, exception = ex.ToString() };

                return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
            }
        }

        // [HttpPost("upload-blob")]
        private async Task<IActionResult> UploadBlob(IFormFile photo, string guid)
        {
            DateTime now = DateTime.Now;

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

            await using var stream = photo.OpenReadStream();
            var uploaded = await containerClient.UploadBlobAsync(guid, stream);

            BlobClient blobClient;

            if (uploaded != null)
            {
                blobClient = containerClient.GetBlobClient(guid);
                return Ok($"Uploaded photo to {blobClient.Uri} at {now}");
            }
            else
            {
                return BadRequest("Backend: failed to upload photo");
            }
        }

        // [HttpPost("extract-exif-metadata")]
        private async Task<Dictionary<string, string>> ExtractExifMetadata(IFormFile photo)
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

        // [HttpPost("store-upload-data")]
        private async Task<ActionResult<UploadForCreationDTO>> StoreUploadData(
            UploadForCreationDTO uploadForCreationDTO
        )
        {
            DateTime now = DateTime.Now;

            try
            {
                _logger.LogInformation("Mapping upload DTO to upload entity");

                var uploadEntity = _mapper.Map<UploadInfo>(uploadForCreationDTO);

                _logger.LogInformation("Creating new upload entity");
                var createdUpload = await _uploadInfoRepo.CreateUploadAsync(uploadEntity);

                if (createdUpload is not UploadInfo)
                {
                    return BadRequest("Failed to store upload data");
                }
                else
                {
                    return CreatedAtAction(
                        "UploadPhoto",
                        new { guid = uploadEntity.AzureStorageId },
                        _mapper.Map<UploadForCreationDTO>(uploadEntity)
                    );
                }
            }
            catch (Exception ex)
            {
                // return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
                throw new Exception("Error uploading to Azure Blob Storage", ex);
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
