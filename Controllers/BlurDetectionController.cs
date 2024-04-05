// using Emgu.CV;
// using Emgu.CV.CvEnum;
// using Emgu.CV.Shape;
// using Emgu.CV.Structure;
// using Microsoft.AspNetCore.Components;
// using Microsoft.AspNetCore.Mvc;
// using System.Drawing;

// namespace BlobTutorial_V2.Controllers
// {
//     [Route("api/blurdetection")]
//     [ApiController]
//     public class BlurDetectionController : ControllerBase
//     {
//         private readonly IConfiguration _configuration;
//         private readonly ILogger<PhotoUploadController> _logger;

//         public BlurDetectionController(
//             IConfiguration configuration,
//             ILogger<PhotoUploadController> logger
//         )
//         {
//             _configuration = configuration;
//             _logger = logger;
//         }

//         [HttpPost]
//         public async Task<bool> IsBlurry(IFormFile image, double threshold = 100.0)
//         {
//             var mat = GetMatFromSDImage(image);
//             var varianceOfLaplacian = VarianceOfLaplacian(mat);
//             return varianceOfLaplacian < threshold;
//         }

//         private static double VarianceOfLaplacian(Mat mat)
//         {
//             using var laplacian = new Mat();
//             CvInvoke.Laplacian(mat, laplacian, DepthType.Cv64F);
//             var mean = new MCvScalar();
//             var stddev = new MCvScalar();
//             CvInvoke.MeanStdDev(laplacian, ref mean, ref stddev);
//             return stddev.V0 * stddev.V0;
//         }

//         private static Mat GetMatFromSDImage(IFormFile image)
//         {
//             int stride = 0;
//             Bitmap bmp = new Bitmap(image);

//             System.Drawing.Rectangle rect = new System.Drawing.Rectangle(
//                 0,
//                 0,
//                 bmp.Width,
//                 bmp.Height
//             );
//             System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(
//                 rect,
//                 System.Drawing.Imaging.ImageLockMode.ReadWrite,
//                 bmp.PixelFormat
//             );

//             System.Drawing.Imaging.PixelFormat pf = bmp.PixelFormat;
//             if (pf == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
//             {
//                 stride = bmp.Width * 4;
//             }
//             else
//             {
//                 stride = bmp.Width * 3;
//             }

//             Image<Bgra, byte> cvImage = new Image<Bgra, byte>(
//                 bmp.Width,
//                 bmp.Height,
//                 stride,
//                 (IntPtr)bmpData.Scan0
//             );

//             bmp.UnlockBits(bmpData);

//             return cvImage.Mat;
//         }
//     }
// }



// using Emgu.CV;
// using Emgu.CV.CvEnum;
// using Emgu.CV.Structure;
// using Microsoft.AspNetCore.Mvc;

// namespace ImageBlurDetection.Controllers
// {
//     [ApiController]
//     [Route("api/imageblurdetection")]
//     public class ImageBlurDetectionController : ControllerBase
//     {
//         [HttpPost]
//         [Route("IsBlurry")]
//         public IActionResult IsBlurry([FromBody] byte[] imageData)
//         {
//             try
//             {
//                 using (Mat mat = GetMatFromImage(imageData))
//                 {
//                     bool isBlurry = mat.IsBlurry();
//                     return Ok(new { IsBlurry = isBlurry });
//                 }
//             }
//             catch
//             {
//                 return BadRequest("Invalid image data.");
//             }
//         }

//         private Mat GetMatFromImage(byte[] imageData)
//         {
//             Mat mat = new Mat();
//             CvInvoke.Imdecode(imageData, Emgu.CV.CvEnum.ImreadModes.AnyColor, mat);
//             return mat;
//         }
//     }

//     public static class ImageExtensions
//     {
//         public static bool IsBlurry(this Mat mat, double threshold = 100.0)
//         {
//             var varianceOfLaplacian = VarianceOfLaplacian(mat);
//             return varianceOfLaplacian < threshold;
//         }

//         private static double VarianceOfLaplacian(Mat mat)
//         {
//             using (var laplacian = new Mat())
//             {
//                 CvInvoke.Laplacian(mat, laplacian, DepthType.Cv64F);
//                 var mean = new MCvScalar();
//                 var stddev = new MCvScalar();
//                 CvInvoke.MeanStdDev(laplacian, ref mean, ref stddev);
//                 return stddev.V0 * stddev.V0;
//             }
//         }
//     }
// }

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
