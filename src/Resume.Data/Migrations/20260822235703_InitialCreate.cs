using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Resume.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Education",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Institution = table.Column<string>(type: "text", nullable: false),
                    Degree = table.Column<string>(type: "text", nullable: false),
                    FieldOfStudy = table.Column<string>(type: "text", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Details = table.Column<string[]>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Education", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Experience",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Company = table.Column<string>(type: "text", nullable: false),
                    JobTitle = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Highlights = table.Column<string[]>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Experience", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersonalInfo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Headline = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    LinkedInUrl = table.Column<string>(type: "text", nullable: true),
                    GitHubUrl = table.Column<string>(type: "text", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Education",
                columns: new[] { "Id", "Degree", "Details", "EndDate", "FieldOfStudy", "Institution", "StartDate" },
                values: new object[] { new Guid("44444444-4444-4444-4444-444444444401"), "B.S.", new[] { "Graduated cum laude", "Teaching assistant for Data Structures" }, new DateOnly(2018, 5, 1), "Computer Science", "State University", new DateOnly(2014, 9, 1) });

            migrationBuilder.InsertData(
                table: "Experience",
                columns: new[] { "Id", "Company", "EndDate", "Highlights", "JobTitle", "Location", "StartDate" },
                values: new object[,]
                {
                    { new Guid("33333333-3333-3333-3333-333333333301"), "Northwind Traders", null, new[] { "Led migration of a monolithic ASP.NET application to a vertical-slice API architecture.", "Designed and shipped an internal developer platform used by 40+ engineers.", "Mentored 3 junior engineers through structured code review and pairing." }, "Senior Software Engineer", "Remote", new DateOnly(2022, 3, 1) },
                    { new Guid("33333333-3333-3333-3333-333333333302"), "Contoso Software", new DateOnly(2022, 2, 1), new[] { "Built and maintained a customer-facing React application serving 200k monthly users.", "Introduced automated integration testing, cutting production incidents by 30%." }, "Software Engineer", "Seattle, WA", new DateOnly(2018, 6, 1) }
                });

            migrationBuilder.InsertData(
                table: "PersonalInfo",
                columns: new[] { "Id", "Email", "FullName", "GitHubUrl", "Headline", "LinkedInUrl", "Phone", "Summary", "WebsiteUrl" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "jordan.rivera@example.com", "Jordan Rivera", "https://github.com/jordanrivera", "Senior Software Engineer", "https://linkedin.com/in/jordanrivera", "555-0100", "Backend-leaning full-stack engineer with 10+ years building distributed systems, developer tooling, and web platforms.", "https://jordanrivera.dev" });

            migrationBuilder.InsertData(
                table: "Skills",
                columns: new[] { "Id", "Category", "Name", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222201"), "Languages", "C#", 1 },
                    { new Guid("22222222-2222-2222-2222-222222222202"), "Languages", "TypeScript", 2 },
                    { new Guid("22222222-2222-2222-2222-222222222203"), "Languages", "SQL", 3 },
                    { new Guid("22222222-2222-2222-2222-222222222204"), "Frameworks", "ASP.NET Core", 1 },
                    { new Guid("22222222-2222-2222-2222-222222222205"), "Frameworks", "React", 2 },
                    { new Guid("22222222-2222-2222-2222-222222222206"), "Frameworks", "Blazor", 3 },
                    { new Guid("22222222-2222-2222-2222-222222222207"), "Tools", "Docker", 1 },
                    { new Guid("22222222-2222-2222-2222-222222222208"), "Tools", "Git", 2 },
                    { new Guid("22222222-2222-2222-2222-222222222209"), "Cloud", "Azure", 1 },
                    { new Guid("22222222-2222-2222-2222-22222222220a"), "Cloud", "AWS", 2 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Education");

            migrationBuilder.DropTable(
                name: "Experience");

            migrationBuilder.DropTable(
                name: "PersonalInfo");

            migrationBuilder.DropTable(
                name: "Skills");
        }
    }
}
