using System;
using System.IO;
using Emgu.CV;
using Emgu.CV.Structure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BlurDetection.Controllers
{
    [ApiController]
    [Route("api/blurdetection")]
    public class BlurDetectionController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BlurDetectionController> _logger;

        public BlurDetectionController(
            IConfiguration configuration,
            ILogger<BlurDetectionController> logger
        )
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost]
        [Route("IsBlurry")]
        public IActionResult IsBlurry(IFormFile imageFile)
        {
            try
            {
                if (imageFile == null || imageFile.Length == 0)
                {
                    return BadRequest("No image file uploaded.");
                }

                using (Stream stream = imageFile.OpenReadStream())
                {
                    using (Mat mat = GetMatFromImage(stream))
                    {
                        bool isBlurry = mat.IsBlurry();
                        return Ok(new { IsBlurry = isBlurry });
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Error processing image: " + ex.Message);
            }
        }

        private Mat GetMatFromImage(Stream stream)
        {
            Mat mat = new Mat();
            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                byte[] imageData = ms.ToArray();



                CvInvoke.Imdecode(imageData, Emgu.CV.CvEnum.ImreadModes.AnyColor, mat);
            }
            return mat;
        }
    }

    public static class ImageExtensions
    {
        public static bool IsBlurry(this Mat mat, double threshold = 100.0)
        {
            var varianceOfLaplacian = VarianceOfLaplacian(mat);
            Console.WriteLine("Variance of Laplacian: " + varianceOfLaplacian);
            return varianceOfLaplacian < threshold;
        }

        private static double VarianceOfLaplacian(Mat mat)
        {
            using (var laplacian = new Mat())
            {
                CvInvoke.Laplacian(mat, laplacian, Emgu.CV.CvEnum.DepthType.Cv64F);
                var mean = new MCvScalar();
                var stddev = new MCvScalar();
                CvInvoke.MeanStdDev(laplacian, ref mean, ref stddev);
                return stddev.V0 * stddev.V0;
            }
        }
    }
}
