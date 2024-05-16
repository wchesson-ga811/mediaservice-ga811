using System;
using System.Collections.Generic;
using ImageUpload.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediaService.DbContexts
{
    public class MediaServiceContext : DbContext
    {
        public DbSet<UploadInfo> UploadInfo { get; set; }

        public MediaServiceContext(DbContextOptions<MediaServiceContext> options)
            : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.LogTo(
                Console.WriteLine,
                new[] { DbLoggerCategory.Database.Command.Name }
            );
        }
    }
}
