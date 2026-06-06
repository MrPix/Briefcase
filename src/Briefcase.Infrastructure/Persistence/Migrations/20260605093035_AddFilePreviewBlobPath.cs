using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Briefcase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFilePreviewBlobPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreviewBlobPath",
                table: "FileAttachments",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreviewBlobPath",
                table: "FileAttachments");
        }
    }
}
