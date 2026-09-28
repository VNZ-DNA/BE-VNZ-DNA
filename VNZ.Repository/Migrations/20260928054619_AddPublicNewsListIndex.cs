using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VNZ.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicNewsListIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_News_Article_Public_PublishAt_Id",
                table: "News_Article",
                columns: new[] { "PublishAt", "Id" },
                filter: "\"Status\" = 'Published' AND \"Published\" = TRUE AND \"PublishAt\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_News_Article_Public_PublishAt_Id",
                table: "News_Article");
        }
    }
}
