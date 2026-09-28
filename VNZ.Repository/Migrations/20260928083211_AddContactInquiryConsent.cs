using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VNZ.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddContactInquiryConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CompanyName",
                table: "Contact_Inquiry",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ConsentToDataProcessing",
                table: "Contact_Inquiry",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql(
                "ALTER TABLE \"Contact_Inquiry\" ALTER COLUMN \"ConsentToDataProcessing\" DROP DEFAULT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsentToDataProcessing",
                table: "Contact_Inquiry");

            migrationBuilder.AlterColumn<string>(
                name: "CompanyName",
                table: "Contact_Inquiry",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
