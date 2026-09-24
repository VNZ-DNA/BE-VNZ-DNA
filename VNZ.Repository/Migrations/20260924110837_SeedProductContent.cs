using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VNZ.Repository.Migrations
{
    /// <inheritdoc />
    public partial class SeedProductContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Product"
                SET "Content" = CASE "Id"
                    WHEN '00000000-0000-0000-0000-000000000101' THEN jsonb_build_object(
                        'Blocks', jsonb_build_array(
                            jsonb_build_object('Id', '00000000-0000-0000-0000-000000001001'::uuid, 'Type', 1, 'Order', 1, 'Text', 'VNZ Analytics', 'Items', NULL),
                            jsonb_build_object('Id', '00000000-0000-0000-0000-000000001002'::uuid, 'Type', 2, 'Order', 2, 'Text', 'Nền tảng phân tích dữ liệu cho doanh nghiệp.', 'Items', NULL)
                        )
                    )
                    WHEN '00000000-0000-0000-0000-000000000102' THEN jsonb_build_object(
                        'Blocks', jsonb_build_array(
                            jsonb_build_object('Id', '00000000-0000-0000-0000-000000001003'::uuid, 'Type', 1, 'Order', 1, 'Text', 'VNZ HRM', 'Items', NULL),
                            jsonb_build_object('Id', '00000000-0000-0000-0000-000000001004'::uuid, 'Type', 2, 'Order', 2, 'Text', 'Giải pháp quản trị nhân sự tập trung.', 'Items', NULL)
                        )
                    )
                    WHEN '00000000-0000-0000-0000-000000000103' THEN jsonb_build_object(
                        'Blocks', jsonb_build_array(
                            jsonb_build_object('Id', '00000000-0000-0000-0000-000000001005'::uuid, 'Type', 1, 'Order', 1, 'Text', 'VNZ Portal', 'Items', NULL),
                            jsonb_build_object('Id', '00000000-0000-0000-0000-000000001006'::uuid, 'Type', 2, 'Order', 2, 'Text', 'Cổng thông tin nội bộ của VNZ.', 'Items', NULL)
                        )
                    )
                    WHEN '00000000-0000-0000-0000-000000000104' THEN jsonb_build_object(
                        'Blocks', jsonb_build_array(
                            jsonb_build_object('Id', '00000000-0000-0000-0000-000000001007'::uuid, 'Type', 1, 'Order', 1, 'Text', 'VNZ Academy', 'Items', NULL),
                            jsonb_build_object('Id', '00000000-0000-0000-0000-000000001008'::uuid, 'Type', 2, 'Order', 2, 'Text', 'Nền tảng đào tạo và phát triển năng lực.', 'Items', NULL)
                        )
                    )
                END
                WHERE "Id" IN (
                    '00000000-0000-0000-0000-000000000101',
                    '00000000-0000-0000-0000-000000000102',
                    '00000000-0000-0000-0000-000000000103',
                    '00000000-0000-0000-0000-000000000104'
                )
                AND "Content" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
