using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VNZ.Repository.Migrations
{
    /// <inheritdoc />
    public partial class BackfillEmptyJobPostSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Job_Application" AS application
                SET "JobPostSnapshot" = jsonb_build_object(
                    'DepartmentId', COALESCE(job."DepartmentId", '00000000-0000-0000-0000-000000000000'::uuid),
                    'DepartmentName', COALESCE(department."Name", ''),
                    'Title', job."Title",
                    'EmploymentType', CASE job."EmploymentType"
                        WHEN 'Internship' THEN 1
                        WHEN 'FullTime' THEN 2
                        WHEN 'PartTime' THEN 3
                        WHEN 'Contract' THEN 4
                        ELSE 0
                    END,
                    'JobLevel', CASE job."JobLevel"
                        WHEN 'Intern' THEN 1
                        WHEN 'Fresher' THEN 2
                        WHEN 'Junior' THEN 3
                        WHEN 'Middle' THEN 4
                        WHEN 'Senior' THEN 5
                        WHEN 'Lead' THEN 6
                        ELSE 0
                    END,
                    'NumberOfPositions', job."NumberOfPositions",
                    'ShortDescription', COALESCE(job."ShortDescription", ''),
                    'Description', COALESCE(job."Description", ''),
                    'Requirements', COALESCE(job."Requirements", ''),
                    'ExpiredAt', job."ExpiredAt",
                    'JobSkillsSnapshot', jsonb_build_object(
                        'Skills', COALESCE(job."Skills", '[]'::jsonb)
                    )
                )
                FROM "Job_Post" AS job
                LEFT JOIN "Department" AS department ON department."Id" = job."DepartmentId"
                WHERE application."JobPostId" = job."Id"
                  AND (
                      application."JobPostSnapshot" IS NULL
                      OR COALESCE(application."JobPostSnapshot"->>'Title', '') = ''
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
