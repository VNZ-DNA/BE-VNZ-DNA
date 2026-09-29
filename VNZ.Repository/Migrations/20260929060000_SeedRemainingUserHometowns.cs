using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VNZ.Repository;

#nullable disable

namespace VNZ.Repository.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260929060000_SeedRemainingUserHometowns")]
public partial class SeedRemainingUserHometowns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "User"
            SET "Hometown" = 'Nhơn Trạch',
                "HometownUrl" = 'https://res.cloudinary.com/vdq3o6gp/image/upload/v1790653633/nhontrach.png'
            WHERE "Id" = '10000000-0000-0000-0000-000000000013';

            UPDATE "User"
            SET "Hometown" = 'Sài Gòn',
                "HometownUrl" = 'https://res.cloudinary.com/vdq3o6gp/image/upload/v1790653634/saigon.png'
            WHERE "Id" = '10000000-0000-0000-0000-000000000010';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "User"
            SET "Hometown" = 'Huế',
                "HometownUrl" = NULL
            WHERE "Id" = '10000000-0000-0000-0000-000000000013';

            UPDATE "User"
            SET "Hometown" = NULL,
                "HometownUrl" = NULL
            WHERE "Id" = '10000000-0000-0000-0000-000000000010';
            """);
    }
}
