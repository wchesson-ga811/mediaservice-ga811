using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Emgu.CV;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.AzureAppServices;
using Serilog;

// Log.Logger = new LoggerConfiguration()
//     .MinimumLevel.Debug()
//     .WriteTo.Console()
//     .WriteTo.File("logs/image-upload-backend.txt", rollingInterval: RollingInterval.Day)
//     .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddAzureWebAppDiagnostics();

//log to filesystem in Azure
builder.Services.Configure<AzureFileLoggerOptions>(options =>
{
    options.FileName = "logs -";
    options.FileSizeLimit = 50 * 1024;
    options.RetainedFileCountLimit = 5;
});

//log to blob storage in Azure
builder.Services.Configure<AzureBlobLoggerOptions>(options =>
{
    options.BlobName = "log.txt";
});

// builder.Host.UseSerilog();
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
app.UseSwagger();
app.UseSwaggerUI();

// }

// app.UseHttpsRedirection();

app.MapControllers();

// var summaries = new[]
// {
//     "Freezing",
//     "Bracing",
//     "Chilly",
//     "Cool",
//     "Mild",
//     "Warm",
//     "Balmy",
//     "Hot",
//     "Sweltering",
//     "Scorching"
// };

// app.MapGet(
//         "/weatherforecast",
//         async () =>
//         {
//             var blobClient = new BlobClient(
//                 new Uri(
//                     "https://mediaservicega811.blob.core.windows.net/images/custom_methods_thumbnail.jpg"
//                 )
//             );
//             //can add StorageSharedKeyCredential inside BlobClient if using an access keyj
//             var imageFile = await blobClient.DownloadContentAsync();
//         }
//     )
//     .WithName("GetWeatherForecast")
//     .WithOpenApi();



// app.MapPost("/weatherforecast", async([FromFormAttribute] FileUploadRequest data) => {
//     var blobServiceClient = new BlobServiceClient(new Uri("https://mediaservicega811.blob.core.windows.net/"), new DefaultAzureCredential());

//     var containerClient = blobServiceClient.GetBlobContainerClient(data.containerName);

//     await using var stream = data.file.OpenReadStream();
//     await containerClient.UploadBlobAsync(data.file.FileName, stream);
// }).
// WithName("PostWeatherForecast").DisableAntiforgery().WithOpenApi();



app.Run();

// record FileUploadRequest(IFormFile file, string containerName) {

// };

// record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
// {
//     public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
// }
