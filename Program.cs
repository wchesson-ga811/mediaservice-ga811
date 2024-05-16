using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Emgu.CV;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.AzureAppServices;
using MediaService.DbContexts;
using MediaService.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/image-upload-backend.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

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

builder.Host.UseSerilog();
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers();


// Install the necessary package for AddDbContext
// Run the following command in the terminal:
// dotnet add package Microsoft.EntityFrameworkCore.SqlServer

builder.Services.AddDbContext<MediaServiceContext>(dbContextOptions =>
    dbContextOptions.UseSqlServer(builder.Configuration.GetConnectionString("MSContext"), options => {
        options.EnableRetryOnFailure();
    })
);

var app = builder.Build();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
app.UseSwagger();
app.UseSwaggerUI();

// }

// app.UseHttpsRedirection();

app.MapControllers();











app.Run();


