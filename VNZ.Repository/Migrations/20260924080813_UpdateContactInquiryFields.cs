using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VNZ.Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateContactInquiryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedStartDate",
                table: "Contact_Inquiry");

            migrationBuilder.AlterColumn<string>(
                name: "ContactStatus",
                table: "Contact_Inquiry",
                type: "text",
                nullable: false,
                defaultValue: "NotContacted",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExpectedStart",
                table: "Contact_Inquiry",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InquiryTopic",
                table: "Contact_Inquiry",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedStart",
                table: "Contact_Inquiry");

            migrationBuilder.DropColumn(
                name: "InquiryTopic",
                table: "Contact_Inquiry");

            migrationBuilder.AlterColumn<string>(
                name: "ContactStatus",
                table: "Contact_Inquiry",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "NotContacted");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpectedStartDate",
                table: "Contact_Inquiry",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
