using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Briefcase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CorrectGoogleMapsDestinationCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                    """
                                UPDATE "Messages"
                                SET "NavigationStatus" = 'Pending',
                                        "NavigationLatitude" = NULL,
                                        "NavigationLongitude" = NULL,
                                        "NavigationProcessingStartedAt" = NULL,
                                        "NavigationProcessedAt" = NULL,
                                        "NavigationProcessingAttempts" = 0,
                                        "NavigationProcessingError" = NULL
                                WHERE "Kind" = 'Url'
                                    AND "IsEncrypted" = FALSE
                                    AND "IsDeleted" = FALSE
                                    AND "IsPermanentlyDeleted" = FALSE
                                    AND "NavigationStatus" IN ('Completed', 'Failed');
                                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
