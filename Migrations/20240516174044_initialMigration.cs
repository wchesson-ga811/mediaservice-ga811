using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediaService.Migrations
{
    /// <inheritdoc />
    public partial class initialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UploadInfo",
                columns: table => new
                {
                    UploadId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AzureStorageId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploaderName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploaderCompany = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploaderEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploaderLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadInfo", x => x.UploadId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UploadInfo");
        }
    }
}
