using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VNZ.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsReadingTimeMinutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReadingTimeMinutes",
                table: "News_Article",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(
                """
                UPDATE "News_Article" AS article
                SET "ReadingTimeMinutes" = GREATEST(
                    1,
                    CEILING(
                        CASE
                            WHEN normalized_content."PlainText" = '' THEN 0
                            ELSE CARDINALITY(
                                REGEXP_SPLIT_TO_ARRAY(normalized_content."PlainText", '\s+')
                            )
                        END / 200.0
                    )::integer
                )
                FROM (
                    SELECT
                        "Id",
                        BTRIM(
                            REGEXP_REPLACE(
                                REGEXP_REPLACE(COALESCE("Content", ''), '<[^>]*>', ' ', 'g'),
                                '\s+',
                                ' ',
                                'g'
                            )
                        ) AS "PlainText"
                    FROM "News_Article"
                ) AS normalized_content
                WHERE article."Id" = normalized_content."Id";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReadingTimeMinutes",
                table: "News_Article");
        }
    }
}
