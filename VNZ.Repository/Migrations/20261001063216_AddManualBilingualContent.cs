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

                UPDATE "News_Article"
                SET "Translations" = $$
                {"en":{"title":"Anthropic accelerates the AI race with Sonnet 5.5 ABC","summary":"<p>Just days after Opus 5.5, Anthropic introduced Sonnet 5.5 with a focus on speed, cost, and everyday work, while the company's leadership continued urging the AI industry to control its development pace.</p>","content":"<p>Less than a week after releasing Opus 5.5, Anthropic introduced Sonnet 5.5. The sequence highlights a notable contradiction: the company says more time is needed for safety mechanisms to keep pace with AI capabilities, yet competitive pressure continues to drive back-to-back releases.</p><h2>Sonnet 5.5 prioritizes speed and efficiency</h2><p>Sonnet 5.5 is positioned as a more practical option than Opus 5.5. The goal is not only to achieve the highest scores, but also to help users handle everyday tasks such as coding, debugging, drafting documents, designing slides, and working with spreadsheets. Anthropic says the new model responds significantly faster than Sonnet 5 and uses fewer tokens, making it suitable for workflows that require multiple rounds of experimentation.</p><p>This positioning shows that a smaller model can still be the better choice when speed and cost matter. Users do not always need the most capable model; sometimes a fast model that is easy to call repeatedly delivers better overall productivity.</p><figure class='news-image'><img src='https://res.cloudinary.com/vdq3o6gp/image/upload/v1790676595/vnz/news/eq1fldhe8wyizvebagpk.png' alt='Benchmark table comparing Sonnet 5.5, Sonnet 5, Opus 5.5, and GPT-6 Sol'></figure>The benchmark table illustrates the relative results of Sonnet 5.5, Sonnet 5, Opus 5.5, and GPT-6 Sol.<h2>The tension between calls to slow down and the release race</h2><p>The notable point is not the new model alone. Earlier, CEO Dario Amodei called on laboratories to actively control the pace of progress so safety work would have time to catch up. Yet in the market, companies continue expanding their model lineups with different sizes and price points.</p><p>Anthropic is building multiple tiers within the same generation: Opus for tasks requiring high capability, Sonnet for work balancing quality and speed, and Haiku for lighter, more cost-efficient needs. This business model gives users a way to choose the right capability level instead of routing every request through a single model.</p><figure class='news-image'><img src='https://res.cloudinary.com/vdq3o6gp/image/upload/v1790676598/vnz/news/fz3k4z5gddryfgx7e0ss.png' alt='Reference pricing table for Claude Sonnet 5.5 and Claude Opus 5.5'></figure>Reference pricing per million tokens shows that Sonnet 5.5 targets cost-efficient workloads.<h2>More than a single model launch</h2><p>Sonnet 5.5 therefore reflects two parallel trends in the AI industry. On one hand, new models are optimized to run faster, cost less, and integrate more easily into products. On the other hand, competition keeps development moving at a rapid pace, even as industry leaders acknowledge that risks and safeguards need more time.</p><p>Anthropic also says Haiku 5.5 will continue to expand the Claude 5.5 line. Once Opus, Sonnet, and Haiku are all available, users will have more choices, while AI labs will need to show that release speed can be matched with an appropriate level of safety.</p><p><strong>References:</strong> <a href='https://genk.vn/keu-goi-nganh-ai-di-cham-lai-nhung-anthropic-lai-tung-lien-tiep-2-model-moi-trong-chua-day-mot-tuan-165260929080643281.chn'>the GenK article</a> and information published by <a href='https://www.anthropic.com/claude-sonnet-5-5'>Anthropic</a>.</p>"}}
                $$::jsonb
                WHERE "Id" = 'bc344d8f-5531-4eef-9c55-f2ae476d4680'
                  AND "Translations" IS NULL
                  AND "Title" = 'Anthropic tăng tốc cuộc đua AI với Sonnet 5.5 ABC';

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
