using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VNZ.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddJobApplicationApplicantDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Availability",
                table: "Job_Application",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "AvailableStartDate",
                table: "Job_Application",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GraduationYear",
                table: "Job_Application",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferralSource",
                table: "Job_Application",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Availability",
                table: "Job_Application");

            migrationBuilder.DropColumn(
                name: "AvailableStartDate",
                table: "Job_Application");

            migrationBuilder.DropColumn(
                name: "GraduationYear",
                table: "Job_Application");

            migrationBuilder.DropColumn(
                name: "ReferralSource",
                table: "Job_Application");
        }
    }
}
