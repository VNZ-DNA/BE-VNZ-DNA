using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VNZ.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddJobApplicationConsentToDataProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ConsentToDataProcessing",
                table: "Job_Application",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                "ALTER TABLE \"Job_Application\" ALTER COLUMN \"ConsentToDataProcessing\" DROP DEFAULT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsentToDataProcessing",
                table: "Job_Application");
        }
    }
}
