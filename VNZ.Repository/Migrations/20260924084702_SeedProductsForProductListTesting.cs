using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VNZ.Repository.Migrations
{
    /// <inheritdoc />
    public partial class SeedProductsForProductListTesting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "Product" (
                    "Id", "Name", "LogoUrl", "ProductUrl", "Status", "IsPublished",
                    "DisplayOrder", "CreatedBy", "CreatedAt", "UpdatedAt", "Content")
                VALUES
                    ('00000000-0000-0000-0000-000000000101', 'VNZ Analytics',
                     'https://cdn.vnz.vn/products/analytics-logo.png',
                     'https://vnz.vn/products/analytics', 'Completed', TRUE, 1, NULL,
                     '2026-09-20T08:00:00+00:00', '2026-09-23T10:00:00+00:00', NULL),
                    ('00000000-0000-0000-0000-000000000102', 'VNZ HRM',
                     'https://cdn.vnz.vn/products/hrm-logo.png',
                     'https://vnz.vn/products/hrm', 'Completed', TRUE, 2, NULL,
                     '2026-09-21T08:00:00+00:00', '2026-09-23T10:00:00+00:00', NULL),
                    ('00000000-0000-0000-0000-000000000103', 'VNZ Portal',
                     'https://cdn.vnz.vn/products/portal-logo.png',
                     'https://vnz.vn/products/portal', 'InProgress', FALSE, NULL, NULL,
                     '2026-09-24T08:00:00+00:00', NULL, NULL),
                    ('00000000-0000-0000-0000-000000000104', 'VNZ Academy',
                     'https://cdn.vnz.vn/products/academy-logo.png',
                     'https://vnz.vn/products/academy', 'InProgress', FALSE, NULL, NULL,
                     '2026-09-24T09:00:00+00:00', NULL, NULL)
                ON CONFLICT ("Id") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "Product"
                WHERE "Id" IN (
                    '00000000-0000-0000-0000-000000000101',
                    '00000000-0000-0000-0000-000000000102',
                    '00000000-0000-0000-0000-000000000103',
                    '00000000-0000-0000-0000-000000000104');
                """);
        }
    }
}
