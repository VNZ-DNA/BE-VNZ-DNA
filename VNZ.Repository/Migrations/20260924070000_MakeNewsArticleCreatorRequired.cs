using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VNZ.Repository;

#nullable disable

namespace VNZ.Repository.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260924070000_MakeNewsArticleCreatorRequired")]
    public partial class MakeNewsArticleCreatorRequired : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "News_Article"
                        WHERE "CreatedBy" IS NULL
                    ) THEN
                        RAISE EXCEPTION 'Cannot require News_Article.CreatedBy while articles without an author exist.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedBy",
                table: "News_Article",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedBy",
                table: "News_Article",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
