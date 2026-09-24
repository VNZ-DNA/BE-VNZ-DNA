using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VNZ.Repository.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeProductPublication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Product"
                SET "IsPublished" = FALSE,
                    "DisplayOrder" = NULL,
                    "UpdatedAt" = NOW()
                WHERE "Status" = 'InProgress'
                  AND "IsPublished" = TRUE;

                WITH ordered_products AS (
                    SELECT "Id",
                           ROW_NUMBER() OVER (
                               ORDER BY "DisplayOrder" NULLS LAST, "CreatedAt", "Id")::integer AS "NewDisplayOrder"
                    FROM "Product"
                    WHERE "IsPublished" = TRUE
                )
                UPDATE "Product" AS product
                SET "DisplayOrder" = ordered_products."NewDisplayOrder",
                    "UpdatedAt" = NOW()
                FROM ordered_products
                WHERE product."Id" = ordered_products."Id"
                  AND product."DisplayOrder" IS DISTINCT FROM ordered_products."NewDisplayOrder";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
