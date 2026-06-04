using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SavedMessages.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPinnedAtToMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PinnedAt",
                table: "Messages",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PinnedAt",
                table: "Messages");
        }
    }
}
