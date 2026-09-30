using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VNZ.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddProductWordmarkUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WordmarkUrl",
                table: "Product",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"Product\" SET \"WordmarkUrl\" = 'https://res.cloudinary.com/vdq3o6gp/image/upload/v1790670774/vnz/products/mo7utcjxxzs2niy3iaym.png' WHERE \"IsPublished\" = TRUE AND (\"WordmarkUrl\" IS NULL OR btrim(\"WordmarkUrl\") = '');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WordmarkUrl",
                table: "Product");
        }
    }
}
