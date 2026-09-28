using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VNZ.Repository.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260928070000_ResetAndSeedCanonicalData")]
public partial class ResetAndSeedCanonicalData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            TRUNCATE TABLE
                "UserSession", "Job_Application", "News_Article_Category", "News_Article",
                "News_Category", "Job_Post", "Contact_Inquiry", "Partner", "Product",
                "Department", "User", "Role"
            RESTART IDENTITY CASCADE;

            INSERT INTO "Role" ("Id", "Type", "CreatedAt", "UpdatedAt") VALUES
            ('10000000-0000-0000-0000-000000000001', 'Admin', '2026-09-01T00:00:00+00:00', NULL);

            INSERT INTO "User" (
                "Id", "RoleId", "CreatedBy", "FullName", "Email", "PasswordHash", "Position",
                "JobLevel", "AvatarUrl", "AnimationUrl", "AudioUrl", "Hometown", "Hobbies",
                "PersonalQuote", "JoinedDate", "IsActive", "EmploymentStatus", "IsPublished",
                "DisplayOrder", "CreateAt", "UpdatedAt", "ResetPasswordCode") VALUES
            ('10000000-0000-0000-0000-000000000010', '10000000-0000-0000-0000-000000000001', NULL,
             'VNZ Demo Admin', 'admin@vnz-demo.local', '$2a$11$0CLvCGPUK1RWUf.JMJa4DuqrH1K18HIgt0UGC6jZJf7v5Ytv2G70S',
             'Quản trị hệ thống', 'Lead', NULL, NULL, NULL, NULL, NULL, NULL,
             '2025-01-01T00:00:00+00:00', TRUE, 'Working', FALSE, NULL,
             '2025-01-01T00:00:00+00:00', NULL, 0),
            ('10000000-0000-0000-0000-000000000011', NULL, '10000000-0000-0000-0000-000000000010',
             'Nguyễn Minh An', 'minh.an@vnz-demo.local', '$2a$11$0CLvCGPUK1RWUf.JMJa4DuqrH1K18HIgt0UGC6jZJf7v5Ytv2G70S',
             'Product Designer', 'Middle', 'https://placehold.co/480x480/183153/ffffff?text=MA', NULL, NULL,
             'Đà Nẵng', 'Thiết kế trải nghiệm, nhiếp ảnh', 'Thiết kế tốt bắt đầu từ việc lắng nghe.',
             '2025-02-10T00:00:00+00:00', TRUE, 'Working', TRUE, 1,
             '2025-02-10T00:00:00+00:00', NULL, 0),
            ('10000000-0000-0000-0000-000000000012', NULL, '10000000-0000-0000-0000-000000000010',
             'Trần Gia Huy', 'gia.huy@vnz-demo.local', '$2a$11$0CLvCGPUK1RWUf.JMJa4DuqrH1K18HIgt0UGC6jZJf7v5Ytv2G70S',
             'Backend Engineer', 'Senior', 'https://placehold.co/480x480/0f766e/ffffff?text=GH', NULL, NULL,
             'Quảng Nam', 'Kiến trúc hệ thống, chạy bộ', 'Đơn giản là một lựa chọn có chủ đích.',
             '2025-03-01T00:00:00+00:00', TRUE, 'Working', TRUE, 2,
             '2025-03-01T00:00:00+00:00', NULL, 0),
            ('10000000-0000-0000-0000-000000000013', NULL, '10000000-0000-0000-0000-000000000010',
             'Lê Bảo Ngọc', 'bao.ngoc@vnz-demo.local', '$2a$11$0CLvCGPUK1RWUf.JMJa4DuqrH1K18HIgt0UGC6jZJf7v5Ytv2G70S',
             'Business Analyst', 'Middle', 'https://placehold.co/480x480/b45309/ffffff?text=BN', NULL, NULL,
             'Huế', 'Đọc sách, phân tích sản phẩm', 'Câu hỏi đúng mở ra hướng đi đúng.',
             '2025-04-15T00:00:00+00:00', TRUE, 'Working', TRUE, 3,
             '2025-04-15T00:00:00+00:00', NULL, 0);

            INSERT INTO "Department" ("Id", "Name", "CreatedAt") VALUES
            ('20000000-0000-0000-0000-000000000001', 'Kỹ thuật', '2026-01-01T00:00:00+00:00'),
            ('20000000-0000-0000-0000-000000000002', 'Sản phẩm', '2026-01-01T00:00:00+00:00'),
            ('20000000-0000-0000-0000-000000000003', 'Vận hành', '2026-01-01T00:00:00+00:00');

            INSERT INTO "Product" ("Id", "Name", "LogoUrl", "ProductUrl", "Status", "IsPublished", "DisplayOrder", "CreatedBy", "CreatedAt", "UpdatedAt", "Content") VALUES
            ('30000000-0000-0000-0000-000000000001', 'VNZ Flow', 'https://placehold.co/320x160/183153/ffffff?text=VNZ+Flow', 'https://demo.vnz.local/products/flow', 'Completed', TRUE, 1, '10000000-0000-0000-0000-000000000010', '2026-04-01T00:00:00+00:00', '2026-06-01T00:00:00+00:00',
             '{"blocks":[{"id":"30000000-0000-0000-0000-000000000011","type":"Title","order":1,"text":"Không gian điều phối công việc cho đội ngũ hiện đại"},{"id":"30000000-0000-0000-0000-000000000012","type":"Description","order":2,"text":"VNZ Flow giúp các nhóm lập kế hoạch, theo dõi tiến độ và biến mục tiêu thành nhịp làm việc minh bạch."},{"id":"30000000-0000-0000-0000-000000000013","type":"Feature","order":3,"items":[{"id":"30000000-0000-0000-0000-000000000014","title":"Theo dõi mục tiêu theo thời gian thực"},{"id":"30000000-0000-0000-0000-000000000015","title":"Không gian cộng tác tập trung"}]}]}'::jsonb),
            ('30000000-0000-0000-0000-000000000002', 'VNZ Insight', 'https://placehold.co/320x160/0f766e/ffffff?text=VNZ+Insight', 'https://demo.vnz.local/products/insight', 'Completed', TRUE, 2, '10000000-0000-0000-0000-000000000010', '2026-05-01T00:00:00+00:00', NULL,
             '{"blocks":[{"id":"30000000-0000-0000-0000-000000000021","type":"Title","order":1,"text":"Dữ liệu rõ ràng để ra quyết định tự tin"},{"id":"30000000-0000-0000-0000-000000000022","type":"Description","order":2,"text":"VNZ Insight kết nối chỉ số vận hành và chuyển chúng thành góc nhìn dễ hành động cho đội ngũ."}]}'::jsonb);

            INSERT INTO "Partner" ("Id", "CreatedBy", "Name", "LogoUrl", "WebsiteUrl", "Description", "IsPublished", "DisplayOrder", "CreatedAt", "UpdateAt") VALUES
            ('40000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000010', 'Northstar Studio', 'https://placehold.co/320x160/1e3a5f/ffffff?text=Northstar', 'https://example.com/northstar', 'Đối tác mẫu đồng hành trong các dự án trải nghiệm số và thương hiệu.', TRUE, 1, '2026-05-10T00:00:00+00:00', '2026-05-10T00:00:00+00:00'),
            ('40000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000010', 'Tidal Works', 'https://placehold.co/320x160/0f766e/ffffff?text=Tidal+Works', 'https://example.com/tidal', 'Đối tác mẫu trong vận hành nền tảng và tối ưu quy trình số.', TRUE, 2, '2026-05-12T00:00:00+00:00', '2026-05-12T00:00:00+00:00');

            INSERT INTO "Job_Post" ("Id", "DepartmentId", "CreatedBy", "Status", "ExpiredAt", "CreatedAt", "UpdatedAt", "Title", "EmploymentType", "Skills", "JobLevel", "NumberOfPositions", "ShortDescription", "Description", "Requirements") VALUES
            ('50000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000010', 'Open', '2026-12-31T23:59:59+00:00', '2026-08-01T00:00:00+00:00', NULL, 'Backend Engineer (.NET)', 'FullTime', '[".NET 8","PostgreSQL","REST API"]'::jsonb, 'Junior', 2, 'Xây dựng API ổn định cho các sản phẩm số của VNZ.', 'Bạn sẽ cùng đội ngũ thiết kế, phát triển và cải tiến các dịch vụ backend có khả năng mở rộng.', 'Nắm vững C#, nền tảng HTTP và tinh thần học hỏi trong môi trường cộng tác.'),
            ('50000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000010', 'Open', '2026-12-15T23:59:59+00:00', '2026-08-10T00:00:00+00:00', NULL, 'Product Design Intern', 'Internship', '["Figma","User Research","Design System"]'::jsonb, 'Intern', 1, 'Tham gia thiết kế các trải nghiệm số có ý nghĩa.', 'Bạn sẽ hỗ trợ nghiên cứu người dùng, xây dựng wireframe và hoàn thiện thiết kế cùng mentor.', 'Có portfolio cơ bản, yêu thích giải quyết vấn đề và sẵn sàng nhận phản hồi.');

            INSERT INTO "News_Category" ("Id", "Name", "CreatedAt") VALUES
            ('60000000-0000-0000-0000-000000000001', 'Sản phẩm', '2026-01-01T00:00:00+00:00'),
            ('60000000-0000-0000-0000-000000000002', 'Tuyển dụng', '2026-01-01T00:00:00+00:00'),
            ('60000000-0000-0000-0000-000000000003', 'Góc nhìn', '2026-01-01T00:00:00+00:00');

            INSERT INTO "News_Article" ("Id", "Title", "Summary", "Content", "ImageUrl", "Status", "Published", "CreatedBy", "CreatedAt", "UpdatedAt", "PublishAt") VALUES
            ('70000000-0000-0000-0000-000000000001', 'VNZ mở chương trình thực tập mùa hè 2026', '<p>Chương trình dành cho các bạn muốn học qua sản phẩm thật, với mentor đồng hành và mục tiêu rõ ràng theo từng tuần.</p>', '<p>Mùa hè 2026, VNZ mở chương trình thực tập cho các vị trí Backend, Product Design và Business Analysis. Chúng tôi tin rằng một kỳ thực tập có giá trị không chỉ bắt đầu bằng danh sách công việc, mà bằng bối cảnh đủ rõ để người học biết mình đang đóng góp vào đâu.</p><p>Mỗi thực tập sinh được làm việc cùng một mentor, nhận phản hồi đều đặn và tham gia các buổi chia sẻ nội bộ. Lộ trình được thiết kế để cân bằng giữa nền tảng chuyên môn, kỹ năng cộng tác và khả năng trình bày quyết định của mình.</p><p>Điều chúng tôi tìm kiếm là sự tò mò, tinh thần chủ động và mong muốn tạo ra sản phẩm hữu ích cho người dùng.</p>', 'https://placehold.co/1200x630/b45309/ffffff?text=VNZ+Internship', 'Published', TRUE, '10000000-0000-0000-0000-000000000010', '2026-07-01T00:00:00+00:00', NULL, '2026-08-20T08:00:00+00:00'),
            ('70000000-0000-0000-0000-000000000002', 'VNZ Flow: biến nhịp làm việc rời rạc thành một hệ thống rõ ràng', '<p>Một góc nhìn về cách VNZ Flow giúp đội ngũ nhìn thấy mục tiêu, tiến độ và những điểm cần phối hợp.</p>', '<p>Khi đội ngũ lớn dần, công việc thường phân tán qua nhiều công cụ và nhiều cuộc trao đổi. VNZ Flow được xây dựng để đưa mục tiêu, kế hoạch và tiến độ về một không gian chung, nơi mọi người có thể hiểu việc gì quan trọng nhất ở thời điểm hiện tại.</p><p>Sản phẩm không cố thay thế mọi cách làm việc. Thay vào đó, nó giúp các nhóm tạo nhịp kiểm tra hợp lý, ghi nhận quyết định và nhìn thấy mối liên hệ giữa công việc hằng ngày với kết quả dài hạn.</p><p>Chúng tôi tiếp tục hoàn thiện sản phẩm dựa trên phản hồi của những đội ngũ đang vận hành thật.</p>', 'https://placehold.co/1200x630/183153/ffffff?text=VNZ+Flow', 'Published', TRUE, '10000000-0000-0000-0000-000000000010', '2026-07-15T00:00:00+00:00', NULL, '2026-08-12T08:00:00+00:00'),
            ('70000000-0000-0000-0000-000000000003', 'Công nghệ tốt bắt đầu từ một vấn đề được hiểu đúng', '<p>Đội ngũ VNZ chia sẻ cách biến những câu hỏi chưa rõ ràng thành hướng giải quyết có thể kiểm chứng.</p>', '<p>Công nghệ chỉ thực sự có ý nghĩa khi nó giải quyết được một vấn đề cụ thể trong đời sống hoặc vận hành. Vì vậy, trước khi bàn về công cụ, đội ngũ VNZ bắt đầu bằng việc quan sát bối cảnh, lắng nghe người dùng và xác định điều gì đang làm họ mất thời gian hoặc mất niềm tin.</p><p>Từ đó, nhóm cùng viết ra giả định, thử nghiệm ở quy mô nhỏ và đo lường phản hồi. Cách làm này giúp chúng tôi tránh đầu tư quá sớm vào những tính năng hấp dẫn nhưng không tạo được giá trị dài hạn.</p><p>Một sản phẩm bền vững là sản phẩm luôn giữ được sự tò mò đối với vấn đề của người dùng.</p>', 'https://placehold.co/1200x630/0f766e/ffffff?text=VNZ+Perspective', 'Published', TRUE, '10000000-0000-0000-0000-000000000010', '2026-07-20T00:00:00+00:00', NULL, '2026-08-05T08:00:00+00:00'),
            ('70000000-0000-0000-0000-000000000004', 'Bản tin nội bộ quý III', '<p>Nội dung đang được biên tập.</p>', '<p>Nội dung nội bộ đang được biên tập.</p>', NULL, 'Draft', FALSE, '10000000-0000-0000-0000-000000000010', '2026-08-25T00:00:00+00:00', NULL, NULL),
            ('70000000-0000-0000-0000-000000000005', 'Thông báo chương trình đã kết thúc', '<p>Thông báo lưu trữ.</p>', '<p>Nội dung của chương trình đã kết thúc và được lưu trữ để tham chiếu nội bộ.</p>', NULL, 'Closed', FALSE, '10000000-0000-0000-0000-000000000010', '2026-06-01T00:00:00+00:00', '2026-07-01T00:00:00+00:00', '2026-06-05T08:00:00+00:00');

            INSERT INTO "News_Article_Category" ("Id", "NewsArticleId", "NewsCategoryId") VALUES
            ('80000000-0000-0000-0000-000000000001', '70000000-0000-0000-0000-000000000001', '60000000-0000-0000-0000-000000000002'),
            ('80000000-0000-0000-0000-000000000002', '70000000-0000-0000-0000-000000000001', '60000000-0000-0000-0000-000000000003'),
            ('80000000-0000-0000-0000-000000000003', '70000000-0000-0000-0000-000000000002', '60000000-0000-0000-0000-000000000001'),
            ('80000000-0000-0000-0000-000000000004', '70000000-0000-0000-0000-000000000003', '60000000-0000-0000-0000-000000000003');

            INSERT INTO "Job_Application" ("Id", "JobPostId", "FullName", "Email", "Phone", "University", "Major", "GraduationYear", "Availability", "AvailableStartDate", "ReferralSource", "CvUrl", "PortfolioUrl", "CoverLetter", "JobPostSnapshot", "Status", "ReviewAt", "ReviewedBy", "InterViewAt", "CreatedAt", "UpdateAt") VALUES
            ('90000000-0000-0000-0000-000000000001', '50000000-0000-0000-0000-000000000001', 'Phạm Khánh Linh', 'khanh.linh@example.test', '0901000001', 'Đại học Bách khoa Đà Nẵng', 'Kỹ thuật phần mềm', 2026, 'Toàn thời gian', '2026-09-01', 'Website VNZ', 'https://example.test/cv/khanh-linh.pdf', 'https://example.test/portfolio/khanh-linh', 'Tôi mong muốn phát triển nền tảng backend có chất lượng cùng một đội ngũ giàu tinh thần học hỏi.', '{"departmentId":"20000000-0000-0000-0000-000000000001","departmentName":"Kỹ thuật","title":"Backend Engineer (.NET)","employmentType":"FullTime","jobLevel":"Junior","numberOfPositions":2,"shortDescription":"Xây dựng API ổn định cho các sản phẩm số của VNZ.","description":"Bạn sẽ cùng đội ngũ thiết kế, phát triển và cải tiến các dịch vụ backend có khả năng mở rộng.","requirements":"Nắm vững C#, nền tảng HTTP và tinh thần học hỏi trong môi trường cộng tác.","expiredAt":"2026-12-31T23:59:59+00:00","jobSkillsSnapshot":{"skills":[".NET 8","PostgreSQL","REST API"]}}'::jsonb, 'Pending', NULL, NULL, NULL, '2026-08-22T00:00:00+00:00', '2026-08-22T00:00:00+00:00'),
            ('90000000-0000-0000-0000-000000000002', '50000000-0000-0000-0000-000000000002', 'Đỗ Mai Anh', 'mai.anh@example.test', '0901000002', 'Đại học Duy Tân', 'Thiết kế đồ họa', 2026, 'Bán thời gian', '2026-09-05', 'Giới thiệu', 'https://example.test/cv/mai-anh.pdf', 'https://example.test/portfolio/mai-anh', 'Tôi yêu thích việc biến quan sát về người dùng thành những trải nghiệm dễ hiểu và có ích.', '{"departmentId":"20000000-0000-0000-0000-000000000002","departmentName":"Sản phẩm","title":"Product Design Intern","employmentType":"Internship","jobLevel":"Intern","numberOfPositions":1,"shortDescription":"Tham gia thiết kế các trải nghiệm số có ý nghĩa.","description":"Bạn sẽ hỗ trợ nghiên cứu người dùng, xây dựng wireframe và hoàn thiện thiết kế cùng mentor.","requirements":"Có portfolio cơ bản, yêu thích giải quyết vấn đề và sẵn sàng nhận phản hồi.","expiredAt":"2026-12-15T23:59:59+00:00","jobSkillsSnapshot":{"skills":["Figma","User Research","Design System"]}}'::jsonb, 'Accepted', '2026-08-24T00:00:00+00:00', '10000000-0000-0000-0000-000000000010', '2026-09-02T09:00:00+00:00', '2026-08-21T00:00:00+00:00', '2026-08-24T00:00:00+00:00');

            INSERT INTO "Contact_Inquiry" ("Id", "InquiryTopic", "FullName", "Email", "Phone", "CompanyName", "BudgetRange", "ExpectedStart", "Message", "Source", "IsRead", "ContactStatus", "ContactedBy", "CreatedAt") VALUES
            ('a0000000-0000-0000-0000-000000000001', 'TechnologyConsulting', 'Hoàng Thu Hà', 'thu.ha@example.test', '0902000001', 'Aurora Retail', 'From50To200Million', 'WithinOneToThreeMonths', 'Chúng tôi muốn trao đổi về một nền tảng hỗ trợ đội ngũ vận hành cửa hàng và tổng hợp chỉ số theo ngày.', 'Referral', FALSE, 'NotContacted', NULL, '2026-08-23T00:00:00+00:00'),
            ('a0000000-0000-0000-0000-000000000002', 'PartnershipProject', 'Vũ Đức Minh', 'duc.minh@example.test', '0902000002', 'Harbor Creative', 'Under50Million', 'Exploring', 'Chúng tôi đang tìm đối tác đồng hành trong một dự án trải nghiệm số cho cộng đồng sáng tạo.', 'EventSeminar', TRUE, 'Contacted', '10000000-0000-0000-0000-000000000010', '2026-08-18T00:00:00+00:00');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Migration reset dữ liệu không hỗ trợ rollback tự động.");
    }
}
