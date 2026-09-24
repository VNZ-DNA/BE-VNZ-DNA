using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VNZ.Repository.Migrations
{
    /// <inheritdoc />
    public partial class ChangeJobApplicationAvailableStartDateToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Job_Application"
                ALTER COLUMN "AvailableStartDate" TYPE character varying(100)
                USING "AvailableStartDate"::text;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Job_Application"
                ALTER COLUMN "AvailableStartDate" TYPE date
                USING CASE
                    WHEN "AvailableStartDate" ~ '^\d{4}-\d{2}-\d{2}$'
                    THEN "AvailableStartDate"::date
                    ELSE NULL
                END;
                """);
        }
    }
}
