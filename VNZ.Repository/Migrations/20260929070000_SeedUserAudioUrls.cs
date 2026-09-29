using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VNZ.Repository;

#nullable disable

namespace VNZ.Repository.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260929070000_SeedUserAudioUrls")]
public partial class SeedUserAudioUrls : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "User"
            SET "AudioUrl" = 'https://cdn.vnzdna.com/members/audio/tan.mp3'
            WHERE "AudioUrl" IS DISTINCT FROM 'https://cdn.vnzdna.com/members/audio/tan.mp3';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Previous per-user URLs are unknown; clearing them would lose data again.
        throw new NotSupportedException(
            "SeedUserAudioUrls cannot restore previous audio URLs. Restore them from a database backup.");
    }
}
