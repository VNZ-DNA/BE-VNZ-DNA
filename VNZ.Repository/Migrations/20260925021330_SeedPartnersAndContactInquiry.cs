using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VNZ.Repository.Migrations
{
    /// <inheritdoc />
    public partial class SeedPartnersAndContactInquiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "Partner" (
                    "Id", "Name", "LogoUrl", "WebsiteUrl", "Description", "IsPublished",
                    "DisplayOrder", "CreatedBy", "CreatedAt", "UpdateAt")
                VALUES
                    ('00000000-0000-0000-0000-000000000201', 'FPT Software',
                     'https://placehold.co/240x120/00589c/ffffff?text=FPT+Software',
                     'https://fptsoftware.com/', 'Đối tác cung cấp giải pháp công nghệ và chuyển đổi số.', TRUE, 1, NULL,
                     '2026-09-25T02:00:00+00:00', '2026-09-25T02:00:00+00:00'),
                    ('00000000-0000-0000-0000-000000000202', 'VNPT',
                     'https://placehold.co/240x120/0066b3/ffffff?text=VNPT',
                     'https://vnpt.com.vn/', 'Đối tác hạ tầng viễn thông và dịch vụ số.', TRUE, 2, NULL,
                     '2026-09-25T02:01:00+00:00', '2026-09-25T02:01:00+00:00'),
                    ('00000000-0000-0000-0000-000000000203', 'Viettel Solutions',
                     'https://placehold.co/240x120/e21b23/ffffff?text=Viettel+Solutions',
                     'https://solutions.viettel.vn/', 'Đối tác triển khai nền tảng số cho doanh nghiệp.', TRUE, 3, NULL,
                     '2026-09-25T02:02:00+00:00', '2026-09-25T02:02:00+00:00'),
                    ('00000000-0000-0000-0000-000000000204', 'CMC Global',
                     'https://placehold.co/240x120/0071bc/ffffff?text=CMC+Global',
                     'https://cmcglobal.com.vn/', 'Đối tác phát triển phần mềm và dịch vụ CNTT.', TRUE, 4, NULL,
                     '2026-09-25T02:03:00+00:00', '2026-09-25T02:03:00+00:00')
                ON CONFLICT ("Id") DO NOTHING;

                INSERT INTO "Contact_Inquiry" (
                    "Id", "InquiryTopic", "FullName", "Email", "Phone", "CompanyName",
                    "BudgetRange", "ExpectedStart", "Message", "Source", "IsRead",
                    "ContactStatus", "ContactedBy", "CreatedAt")
                VALUES (
                    '00000000-0000-0000-0000-000000000301', 'TechnologyConsulting', 'Dương Billy',
                    'duongbilly18012004@gmail.com', '0900000000', 'VNZ Test Client',
                    'From50To200Million', 'WithinOneToThreeMonths',
                    'Tôi muốn được tư vấn giải pháp công nghệ cho doanh nghiệp.', 'GoogleSearch', FALSE,
                    'NotContacted', NULL, '2026-09-25T02:05:00+00:00')
                ON CONFLICT ("Id") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "Contact_Inquiry"
                WHERE "Id" = '00000000-0000-0000-0000-000000000301';

                DELETE FROM "Partner"
                WHERE "Id" IN (
                    '00000000-0000-0000-0000-000000000201',
                    '00000000-0000-0000-0000-000000000202',
                    '00000000-0000-0000-0000-000000000203',
                    '00000000-0000-0000-0000-000000000204');
                """);
        }
    }
}
