using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Briefcase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPermanentMessageDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_UserId_IsDeleted_CreatedAt",
                table: "Messages");

            migrationBuilder.AddColumn<bool>(
                name: "IsPermanentlyDeleted",
                table: "Messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PermanentlyDeletedAt",
                table: "Messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_UserId_IsDeleted_IsPermanentlyDeleted_CreatedAt",
                table: "Messages",
                columns: new[] { "UserId", "IsDeleted", "IsPermanentlyDeleted", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_UserId_IsDeleted_IsPermanentlyDeleted_CreatedAt",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "IsPermanentlyDeleted",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "PermanentlyDeletedAt",
                table: "Messages");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_UserId_IsDeleted_CreatedAt",
                table: "Messages",
                columns: new[] { "UserId", "IsDeleted", "CreatedAt" });
        }
    }
}
