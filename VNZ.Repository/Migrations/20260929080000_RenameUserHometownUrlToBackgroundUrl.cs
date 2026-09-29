using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VNZ.Repository;

#nullable disable

namespace VNZ.Repository.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260929080000_RenameUserHometownUrlToBackgroundUrl")]
public partial class RenameUserHometownUrlToBackgroundUrl : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "HometownUrl",
            table: "User",
            newName: "BackgroundUrl");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "BackgroundUrl",
            table: "User",
            newName: "HometownUrl");
    }
}
