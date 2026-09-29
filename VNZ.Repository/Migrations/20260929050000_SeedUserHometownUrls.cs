using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VNZ.Repository;

#nullable disable

namespace VNZ.Repository.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260929050000_SeedUserHometownUrls")]
public partial class SeedUserHometownUrls : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "User"
            SET "HometownUrl" = CASE "Id"
                WHEN '10000000-0000-0000-0000-000000000105' THEN 'https://res.cloudinary.com/vdq3o6gp/image/upload/v1790653633/namdinh.png'
                WHEN '10000000-0000-0000-0000-000000000107' THEN 'https://res.cloudinary.com/vdq3o6gp/image/upload/v1790653632/binhdinh.png'
                WHEN '10000000-0000-0000-0000-000000000103' THEN 'https://res.cloudinary.com/vdq3o6gp/image/upload/v1790653633/binhduong.png'
                WHEN '10000000-0000-0000-0000-000000000104' THEN 'https://res.cloudinary.com/vdq3o6gp/image/upload/v1790653633/haiduong.png'
                WHEN '10000000-0000-0000-0000-000000000101' THEN 'https://res.cloudinary.com/vdq3o6gp/image/upload/v1790653633/nhontrach.png'
                WHEN '10000000-0000-0000-0000-000000000102' THEN 'https://res.cloudinary.com/vdq3o6gp/image/upload/v1790653633/nhatrang.png'
                WHEN '10000000-0000-0000-0000-000000000108' THEN 'https://res.cloudinary.com/vdq3o6gp/image/upload/v1790653634/kontum.png'
                WHEN '10000000-0000-0000-0000-000000000106' THEN 'https://res.cloudinary.com/vdq3o6gp/image/upload/v1790653634/ninhbinh.png'
                WHEN '10000000-0000-0000-0000-000000000011' THEN 'https://res.cloudinary.com/vdq3o6gp/image/upload/v1790653634/quangngai.png'
                WHEN '10000000-0000-0000-0000-000000000012' THEN 'https://res.cloudinary.com/vdq3o6gp/image/upload/v1790653634/saigon.png'
            END
            WHERE "Id" IN (
                '10000000-0000-0000-0000-000000000101',
                '10000000-0000-0000-0000-000000000102',
                '10000000-0000-0000-0000-000000000103',
                '10000000-0000-0000-0000-000000000104',
                '10000000-0000-0000-0000-000000000105',
                '10000000-0000-0000-0000-000000000106',
                '10000000-0000-0000-0000-000000000107',
                '10000000-0000-0000-0000-000000000108',
                '10000000-0000-0000-0000-000000000011',
                '10000000-0000-0000-0000-000000000012');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "User"
            SET "HometownUrl" = NULL
            WHERE "Id" IN (
                '10000000-0000-0000-0000-000000000101',
                '10000000-0000-0000-0000-000000000102',
                '10000000-0000-0000-0000-000000000103',
                '10000000-0000-0000-0000-000000000104',
                '10000000-0000-0000-0000-000000000105',
                '10000000-0000-0000-0000-000000000106',
                '10000000-0000-0000-0000-000000000107',
                '10000000-0000-0000-0000-000000000108',
                '10000000-0000-0000-0000-000000000011',
                '10000000-0000-0000-0000-000000000012');
            """);
    }
}
