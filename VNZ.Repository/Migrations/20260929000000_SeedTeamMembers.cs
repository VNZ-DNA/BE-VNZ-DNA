using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VNZ.Repository;

#nullable disable

namespace VNZ.Repository.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260929000000_SeedTeamMembers")]
public partial class SeedTeamMembers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO "User" (
                "Id", "RoleId", "CreatedBy", "FullName", "Email", "PasswordHash", "Position",
                "JobLevel", "AvatarUrl", "AnimationUrl", "AudioUrl", "Hometown", "Hobbies",
                "PersonalQuote", "JoinedDate", "IsActive", "EmploymentStatus", "IsPublished",
                "DisplayOrder", "CreateAt", "UpdatedAt", "ResetPasswordCode") VALUES
            ('10000000-0000-0000-0000-000000000101', NULL, '10000000-0000-0000-0000-000000000010',
             'Hồ Hoàng Nam', 'ho.hoang.nam@vnz-demo.local', '$2a$11$0CLvCGPUK1RWUf.JMJa4DuqrH1K18HIgt0UGC6jZJf7v5Ytv2G70S',
             'Trưởng nhóm Chiến lược', 'Lead',
             'https://res.cloudinary.com/vdq3o6gp/image/upload/v1790652434/nam.png', NULL, NULL,
             'Bến Tre', 'Câu cá, Bơi lội, Làm vườn', 'Chậm mà chắc, chắc mà bền',
             '2026-09-15T00:00:00+00:00', TRUE, 'Working', TRUE, 4,
             '2026-09-29T00:00:00+00:00', NULL, 0),
            ('10000000-0000-0000-0000-000000000102', NULL, '10000000-0000-0000-0000-000000000010',
             'Võ Gia Huy', 'vo.gia.huy@vnz-demo.local', '$2a$11$0CLvCGPUK1RWUf.JMJa4DuqrH1K18HIgt0UGC6jZJf7v5Ytv2G70S',
             'Trưởng nhóm Chiến lược', 'Lead',
             'https://res.cloudinary.com/vdq3o6gp/image/upload/v1790652434/giahuy.png', NULL, NULL,
             'Nha Trang', 'Bóng đá, Du lịch, Đầu tư', 'Nhìn xa trông rộng, đi trước một bước',
             '2026-07-01T00:00:00+00:00', TRUE, 'Working', TRUE, 5,
             '2026-09-29T00:00:00+00:00', NULL, 0),
            ('10000000-0000-0000-0000-000000000103', NULL, '10000000-0000-0000-0000-000000000010',
             'Nguyễn Tùng Dương', 'nguyen.tung.duong@vnz-demo.local', '$2a$11$0CLvCGPUK1RWUf.JMJa4DuqrH1K18HIgt0UGC6jZJf7v5Ytv2G70S',
             'Kỹ sư Full-Stack', 'Senior',
             'https://res.cloudinary.com/vdq3o6gp/image/upload/v1790652434/duong.png', NULL, NULL,
             'Bình Dương', 'Chạy bộ, Nấu ăn, Podcast', 'Mỗi ngày học thêm một điều mới',
             '2026-07-15T00:00:00+00:00', TRUE, 'Working', TRUE, 6,
             '2026-09-29T00:00:00+00:00', NULL, 0),
            ('10000000-0000-0000-0000-000000000104', NULL, '10000000-0000-0000-0000-000000000010',
             'Nguyễn Đức Bình', 'nguyen.duc.binh@vnz-demo.local', '$2a$11$0CLvCGPUK1RWUf.JMJa4DuqrH1K18HIgt0UGC6jZJf7v5Ytv2G70S',
             'CEO', 'Lead',
             'https://res.cloudinary.com/vdq3o6gp/image/upload/v1790652434/binh.png', NULL, NULL,
             'Hải Dương', 'Đọc sách, Cờ vua, Cà phê', 'Nền móng có vững thì nhà mới cao',
             '2026-06-15T00:00:00+00:00', TRUE, 'Working', TRUE, 7,
             '2026-09-29T00:00:00+00:00', NULL, 0),
            ('10000000-0000-0000-0000-000000000105', NULL, '10000000-0000-0000-0000-000000000010',
             'Trần Đình Thiên Tân', 'tran.dinh.thien.tan@vnz-demo.local', '$2a$11$0CLvCGPUK1RWUf.JMJa4DuqrH1K18HIgt0UGC6jZJf7v5Ytv2G70S',
             'CCO/CTO', 'Lead',
             'https://res.cloudinary.com/vdq3o6gp/image/upload/v1790652434/tan.png', NULL, NULL,
             'Nam Định', 'Lập trình, Thể Thao, Âm Nhạc', 'Sống là phải để lại tiếng thơm cho đời',
             '2026-06-01T00:00:00+00:00', TRUE, 'Working', TRUE, 8,
             '2026-09-29T00:00:00+00:00', NULL, 0),
            ('10000000-0000-0000-0000-000000000106', NULL, '10000000-0000-0000-0000-000000000010',
             'Nguyễn Thu Hương', 'nguyen.thu.huong@vnz-demo.local', '$2a$11$0CLvCGPUK1RWUf.JMJa4DuqrH1K18HIgt0UGC6jZJf7v5Ytv2G70S',
             'Trưởng nhóm Chiến lược', 'Lead',
             'https://res.cloudinary.com/vdq3o6gp/image/upload/v1790652434/nguyenhuong.png', NULL, NULL,
             'Ninh Bình', 'Yoga, Viết lách, Làm bánh', 'Tâm sáng thì việc gì cũng thành',
             '2026-08-12T00:00:00+00:00', TRUE, 'Working', TRUE, 9,
             '2026-09-29T00:00:00+00:00', NULL, 0),
            ('10000000-0000-0000-0000-000000000107', NULL, '10000000-0000-0000-0000-000000000010',
             'Nguyễn Tấn Hùng', 'nguyen.tan.hung@vnz-demo.local', '$2a$11$0CLvCGPUK1RWUf.JMJa4DuqrH1K18HIgt0UGC6jZJf7v5Ytv2G70S',
             'Trưởng nhóm Chiến lược', 'Lead',
             'https://res.cloudinary.com/vdq3o6gp/image/upload/v1790652435/tanhung.png', NULL, NULL,
             'Bình Định', 'Guitar, Phượt, Bóng rổ', 'Làm hết sức, chơi hết mình',
             '2026-09-01T00:00:00+00:00', TRUE, 'Working', TRUE, 10,
             '2026-09-29T00:00:00+00:00', NULL, 0),
            ('10000000-0000-0000-0000-000000000108', NULL, '10000000-0000-0000-0000-000000000010',
             'Lưu Hà Vi', 'luu.ha.vi@vnz-demo.local', '$2a$11$0CLvCGPUK1RWUf.JMJa4DuqrH1K18HIgt0UGC6jZJf7v5Ytv2G70S',
             'Product Designer', 'Middle',
             'https://res.cloudinary.com/vdq3o6gp/image/upload/v1790652435/vi.png', NULL, NULL,
             'Kon Tum', 'Vẽ tranh, Nhiếp ảnh, Cây cảnh', 'Đơn giản là đỉnh cao của sự tinh tế',
             '2026-06-20T00:00:00+00:00', TRUE, 'Working', TRUE, 11,
             '2026-09-29T00:00:00+00:00', NULL, 0)
            ON CONFLICT ("Id") DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM "User"
            WHERE "Id" IN (
                '10000000-0000-0000-0000-000000000101',
                '10000000-0000-0000-0000-000000000102',
                '10000000-0000-0000-0000-000000000103',
                '10000000-0000-0000-0000-000000000104',
                '10000000-0000-0000-0000-000000000105',
                '10000000-0000-0000-0000-000000000106',
                '10000000-0000-0000-0000-000000000107',
                '10000000-0000-0000-0000-000000000108');
            """);
    }
}
