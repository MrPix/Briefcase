using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Briefcase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleMapsNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "GoogleMapsNavigationEnabled",
                table: "UserSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "NavigationApplicationIds",
                table: "UserSettings",
                type: "text",
                nullable: false,
                defaultValue: "[\"google-maps\",\"waze\",\"locus-map\",\"maps-me\"]");

            migrationBuilder.AddColumn<double>(
                name: "NavigationLatitude",
                table: "Messages",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "NavigationLongitude",
                table: "Messages",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NavigationProcessedAt",
                table: "Messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NavigationProcessingAttempts",
                table: "Messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NavigationProcessingError",
                table: "Messages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NavigationProcessingStartedAt",
                table: "Messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NavigationStatus",
                table: "Messages",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_NavigationStatus_NavigationProcessingStartedAt",
                table: "Messages",
                columns: new[] { "NavigationStatus", "NavigationProcessingStartedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_NavigationStatus_NavigationProcessingStartedAt",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "GoogleMapsNavigationEnabled",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "NavigationApplicationIds",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "NavigationLatitude",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "NavigationLongitude",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "NavigationProcessedAt",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "NavigationProcessingAttempts",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "NavigationProcessingError",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "NavigationProcessingStartedAt",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "NavigationStatus",
                table: "Messages");
        }
    }
}
