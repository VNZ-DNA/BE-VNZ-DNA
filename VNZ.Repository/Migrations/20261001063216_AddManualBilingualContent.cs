using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VNZ.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddManualBilingualContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Translations",
                table: "Product",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "News_Category",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "News_Article",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AddColumn<string>(
                name: "Translations",
                table: "News_Article",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Job_Post",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AddColumn<string>(
                name: "Translations",
                table: "Job_Post",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Department",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Department"
                SET "Code" = CASE "Id"
                    WHEN '20000000-0000-0000-0000-000000000001' THEN 'ENGINEERING'
                    WHEN '20000000-0000-0000-0000-000000000002' THEN 'PRODUCT'
                    WHEN '20000000-0000-0000-0000-000000000003' THEN 'OPERATIONS'
                    ELSE "Code"
                END
                WHERE "Code" IS NULL OR btrim("Code") = '';

                UPDATE "News_Category"
                SET "Code" = CASE "Id"
                    WHEN '60000000-0000-0000-0000-000000000001' THEN 'PRODUCT'
                    WHEN '60000000-0000-0000-0000-000000000002' THEN 'RECRUITMENT'
                    WHEN '60000000-0000-0000-0000-000000000003' THEN 'PERSPECTIVE'
                    ELSE "Code"
                END
                WHERE "Code" IS NULL OR btrim("Code") = '';

                UPDATE "News_Article"
                SET "Translations" = $$
                {"en":{"title":"VNZ opens the 2026 summer internship program","summary":"<p>A program for people who want to learn through real products, with mentors and clear weekly goals.</p>","content":"<p>In summer 2026, VNZ is opening internship positions in Backend, Product Design, and Business Analysis. We believe a valuable internship starts with more than a task list: it starts with enough context for learners to understand where their work contributes.</p><p>Each intern works with a mentor, receives regular feedback, and joins internal knowledge-sharing sessions. The program balances professional foundations, collaboration skills, and the ability to explain decisions clearly.</p><p>We are looking for curiosity, initiative, and a desire to create useful products for people. Interns will learn by contributing to real work while receiving practical guidance throughout the program.</p>"}}
                $$::jsonb
                WHERE "Id" = '70000000-0000-0000-0000-000000000001'
                  AND "Translations" IS NULL
                  AND "Title" = 'VNZ mở chương trình thực tập mùa hè 2026';

                UPDATE "News_Article"
                SET "Translations" = $$
                {"en":{"title":"VNZ Flow: turning scattered work rhythms into a clear system","summary":"<p>A look at how VNZ Flow helps teams see goals, progress, and the points that need coordination.</p>","content":"<p>As teams grow, work is often spread across many tools and conversations. VNZ Flow brings goals, plans, and progress into one shared space so everyone can understand what matters most right now.</p><p>The product is not meant to replace every way of working. Instead, it helps teams create a practical review rhythm, record decisions, and see the connection between daily work and long-term outcomes.</p><p>We continue improving the product based on feedback from teams that use it in real operations. The goal is a clearer way to work together without losing the context behind each decision.</p>"}}
                $$::jsonb
                WHERE "Id" = '70000000-0000-0000-0000-000000000002'
                  AND "Translations" IS NULL
                  AND "Title" = 'VNZ Flow: biến nhịp làm việc rời rạc thành một hệ thống rõ ràng';

                UPDATE "News_Article"
                SET "Translations" = $$
                {"en":{"title":"Good technology starts with a problem understood well","summary":"<p>The VNZ team shares how unclear questions can become solutions that are possible to validate.</p>","content":"<p>Technology becomes meaningful when it solves a concrete problem in daily life or operations. Before discussing tools, the VNZ team starts by observing context, listening to users, and identifying what is costing them time or trust.</p><p>The team then writes down assumptions, tests them at a small scale, and measures feedback. This approach helps us avoid investing too early in attractive features that do not create lasting value.</p><p>A sustainable product keeps its curiosity about the problems people face. We use that curiosity to make decisions that are useful, measurable, and grounded in real experience.</p>"}}
                $$::jsonb
                WHERE "Id" = '70000000-0000-0000-0000-000000000003'
                  AND "Translations" IS NULL
                  AND "Title" = 'Công nghệ tốt bắt đầu từ một vấn đề được hiểu đúng';

                UPDATE "Job_Post"
                SET "Translations" = $$
                {"en":{"title":"Backend Engineer (.NET)","shortDescription":"Build reliable APIs for VNZ digital products.","description":"You will work with the team to design, develop, and improve scalable backend services.","requirements":"Strong C# and HTTP fundamentals, together with a willingness to learn in a collaborative environment."}}
                $$::jsonb
                WHERE "Id" = '50000000-0000-0000-0000-000000000001'
                  AND "Translations" IS NULL
                  AND "Title" = 'Backend Engineer (.NET)'
                  AND "ShortDescription" = 'Xây dựng API ổn định cho các sản phẩm số của VNZ.';

                UPDATE "Job_Post"
                SET "Translations" = $$
                {"en":{"title":"Product Design Intern","shortDescription":"Help design meaningful digital experiences.","description":"You will support user research, create wireframes, and refine designs with a mentor.","requirements":"A basic portfolio, a passion for problem solving, and openness to feedback."}}
                $$::jsonb
                WHERE "Id" = '50000000-0000-0000-0000-000000000002'
                  AND "Translations" IS NULL
                  AND "Title" = 'Product Design Intern'
                  AND "ShortDescription" = 'Tham gia thiết kế các trải nghiệm số có ý nghĩa.';

                UPDATE "Product"
                SET "Translations" = $$
                {"en":{"content":{"blocks":[{"id":"30000000-0000-0000-0000-000000000011","type":"Title","order":1,"text":"A work coordination space for modern teams"},{"id":"30000000-0000-0000-0000-000000000012","type":"Description","order":2,"text":"VNZ Flow helps teams plan, track progress, and turn goals into a transparent working rhythm."},{"id":"30000000-0000-0000-0000-000000000013","type":"Feature","order":3,"items":[{"id":"30000000-0000-0000-0000-000000000014","title":"Track goals in real time"},{"id":"30000000-0000-0000-0000-000000000015","title":"A focused collaboration space"}]}]}}}
                $$::jsonb
                WHERE "Id" = '30000000-0000-0000-0000-000000000001'
                  AND "Translations" IS NULL
                  AND "Name" = 'VNZ Flow'
                  AND "Content"->'blocks'->0->>'text' = 'Không gian điều phối công việc cho đội ngũ hiện đại';

                UPDATE "Product"
                SET "Translations" = $$
                {"en":{"content":{"blocks":[{"id":"30000000-0000-0000-0000-000000000021","type":"Title","order":1,"text":"Clear data for confident decisions"},{"id":"30000000-0000-0000-0000-000000000022","type":"Description","order":2,"text":"VNZ Insight connects operational metrics and turns them into actionable perspectives for teams."}]}}}
                $$::jsonb
                WHERE "Id" = '30000000-0000-0000-0000-000000000002'
                  AND "Translations" IS NULL
                  AND "Name" = 'VNZ Insight'
                  AND "Content"->'blocks'->0->>'text' = 'Dữ liệu rõ ràng để ra quyết định tự tin';

                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM "Department"
                        WHERE "Code" IS NULL OR btrim("Code") = ''
                    ) THEN
                        RAISE EXCEPTION 'Bilingual migration requires a stable code for every Department';
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM "News_Category"
                        WHERE "Code" IS NULL OR btrim("Code") = ''
                    ) THEN
                        RAISE EXCEPTION 'Bilingual migration requires a stable code for every NewsCategory';
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM "News_Article"
                        WHERE "Status" = 'Published' AND "Published" = TRUE
                          AND (
                              "Title" IS NULL OR btrim("Title") = '' OR
                              "Summary" IS NULL OR btrim("Summary") = '' OR
                              "Content" IS NULL OR btrim("Content") = '' OR
                              jsonb_typeof("Translations"->'en') <> 'object' OR
                              nullif(btrim("Translations"#>>'{en,title}'), '') IS NULL OR
                              nullif(btrim("Translations"#>>'{en,summary}'), '') IS NULL OR
                              nullif(btrim("Translations"#>>'{en,content}'), '') IS NULL
                          )
                    ) THEN
                        RAISE EXCEPTION 'Bilingual migration requires complete VI and EN content for every Published News';
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM "Job_Post"
                        WHERE "Status" = 'Open'
                          AND (
                              "Title" IS NULL OR btrim("Title") = '' OR
                              "ShortDescription" IS NULL OR btrim("ShortDescription") = '' OR
                              "Description" IS NULL OR btrim("Description") = '' OR
                              "Requirements" IS NULL OR btrim("Requirements") = '' OR
                              jsonb_typeof("Translations"->'en') <> 'object' OR
                              nullif(btrim("Translations"#>>'{en,title}'), '') IS NULL OR
                              nullif(btrim("Translations"#>>'{en,shortDescription}'), '') IS NULL OR
                              nullif(btrim("Translations"#>>'{en,description}'), '') IS NULL OR
                              nullif(btrim("Translations"#>>'{en,requirements}'), '') IS NULL
                          )
                    ) THEN
                        RAISE EXCEPTION 'Bilingual migration requires complete VI and EN content for every Open JobPost';
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM "Product"
                        WHERE "IsPublished" = TRUE AND "Content" IS NOT NULL
                          AND (
                              coalesce(jsonb_typeof("Translations"->'en'->'content'), 'null') <> 'object' OR
                              coalesce(jsonb_typeof("Translations"->'en'->'content'->'blocks'), 'null') <> 'array'
                          )
                    ) THEN
                        RAISE EXCEPTION 'Bilingual migration requires EN Product content for every published Product';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "News_Category",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Department",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_News_Category_Code",
                table: "News_Category",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Department_Code",
                table: "Department",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_News_Category_Code",
                table: "News_Category");

            migrationBuilder.DropIndex(
                name: "IX_Department_Code",
                table: "Department");

            migrationBuilder.DropColumn(
                name: "Translations",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "News_Category");

            migrationBuilder.DropColumn(
                name: "Translations",
                table: "News_Article");

            migrationBuilder.DropColumn(
                name: "Translations",
                table: "Job_Post");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Department");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "News_Article",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Job_Post",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true);
        }
    }
}
