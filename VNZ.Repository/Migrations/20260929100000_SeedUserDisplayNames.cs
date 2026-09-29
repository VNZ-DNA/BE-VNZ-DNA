using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VNZ.Repository;

#nullable disable

namespace VNZ.Repository.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260929100000_SeedUserDisplayNames")]
public partial class SeedUserDisplayNames : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "User"
            SET "DisplayName" = CASE "Id"
                WHEN '10000000-0000-0000-0000-000000000011' THEN 'AN'
                WHEN '10000000-0000-0000-0000-000000000012' THEN 'HUY'
                WHEN '10000000-0000-0000-0000-000000000013' THEN 'NGOC'
                WHEN '10000000-0000-0000-0000-000000000101' THEN 'NAM'
                WHEN '10000000-0000-0000-0000-000000000102' THEN 'HUY'
                WHEN '10000000-0000-0000-0000-000000000103' THEN 'DUONG'
                WHEN '10000000-0000-0000-0000-000000000104' THEN 'BINH'
                WHEN '10000000-0000-0000-0000-000000000105' THEN 'TAN'
                WHEN '10000000-0000-0000-0000-000000000106' THEN 'HUONG'
                WHEN '10000000-0000-0000-0000-000000000107' THEN 'HUNG'
                WHEN '10000000-0000-0000-0000-000000000108' THEN 'VI'
            END
            WHERE "RoleId" IS NULL
              AND "DisplayName" IS NULL
              AND "Id" IN (
                '10000000-0000-0000-0000-000000000011',
                '10000000-0000-0000-0000-000000000012',
                '10000000-0000-0000-0000-000000000013',
                '10000000-0000-0000-0000-000000000101',
                '10000000-0000-0000-0000-000000000102',
                '10000000-0000-0000-0000-000000000103',
                '10000000-0000-0000-0000-000000000104',
                '10000000-0000-0000-0000-000000000105',
                '10000000-0000-0000-0000-000000000106',
                '10000000-0000-0000-0000-000000000107',
                '10000000-0000-0000-0000-000000000108');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Previous values are unknown. Do not clear nicknames that may have
        // been entered or changed after this backfill was applied.
        throw new NotSupportedException(
            "SeedUserDisplayNames cannot restore previous display names. Restore them from a database backup.");
    }
}
