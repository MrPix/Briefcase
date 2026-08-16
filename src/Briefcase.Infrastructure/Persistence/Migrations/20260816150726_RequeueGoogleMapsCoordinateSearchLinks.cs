using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Briefcase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RequeueGoogleMapsCoordinateSearchLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                    """
                                UPDATE "Messages"
                                SET "NavigationStatus" = 'Pending',
                                        "NavigationProcessingStartedAt" = NULL,
                                        "NavigationProcessedAt" = NULL,
                                        "NavigationProcessingAttempts" = 0,
                                        "NavigationProcessingError" = NULL
                                WHERE "Kind" = 'Url'
                                    AND "IsEncrypted" = FALSE
                                    AND "IsDeleted" = FALSE
                                    AND "IsPermanentlyDeleted" = FALSE
                                    AND "NavigationStatus" = 'Failed'
                                    AND "Content" LIKE 'https://maps.app.goo.gl/%';
                                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
