using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Resume.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalInfoLocationAndRealResumeData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222201"));

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222202"));

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222203"));

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222204"));

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222205"));

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222206"));

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222207"));

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222208"));

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222209"));

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-22222222220a"));

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "PersonalInfo",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Education",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444401"),
                columns: new[] { "Degree", "Details", "EndDate", "FieldOfStudy", "Institution", "StartDate" },
                values: new object[] { "CIS", new string[0], new DateOnly(2013, 1, 1), "Database Administration", "DeVry University", new DateOnly(2010, 1, 1) });

            migrationBuilder.InsertData(
                table: "Education",
                columns: new[] { "Id", "Degree", "Details", "EndDate", "FieldOfStudy", "Institution", "StartDate" },
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444402"), "CIS", new string[0], new DateOnly(2005, 1, 1), null, "Coleman College", new DateOnly(2004, 1, 1) },
                    { new Guid("44444444-4444-4444-4444-444444444403"), "Telephone/Switchboard Repair Course, Basic Electronics Course, Fundamentals of Leadership Course", new string[0], new DateOnly(2000, 1, 1), null, "U.S. Marine Corps", new DateOnly(1995, 1, 1) }
                });

            migrationBuilder.UpdateData(
                table: "Experience",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333301"),
                columns: new[] { "Company", "Highlights", "JobTitle", "Location", "StartDate" },
                values: new object[] { "AFG Companies", new[] { "Drove migration of a legacy web application from .NET Framework 4.8 to .NET 8 to completion.", "Supported and enhanced a legacy system to improve stability and performance.", "Supported applications using a mix of MVC, Angular, WebAPI, bootstrap, tailwind, and AWS services.", "Implemented repository, chain-of-responsibility, REPR endpoint, vertical slice, and rule engine design patterns to restructure processing logic.", "Optimized and refactored SQL Server stored procedures to eliminate redundant logic and improve maintainability.", "Consolidated and eliminated redundant API services to streamline processing.", "Wrote Terraform IaC to provision AWS resources, including deployment pipelines, Lambda functions, and ECR/ECS services.", "Configured Route 53 DNS routing rules to ELB instances.", "Designed and developed new REST API services using FastEndpoints, M2M Auth0 authentication, and a PostgreSQL backend." }, "Lead/Senior Software Engineer (Hybrid)", null, new DateOnly(2024, 11, 1) });

            migrationBuilder.UpdateData(
                table: "Experience",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333302"),
                columns: new[] { "Company", "EndDate", "Highlights", "JobTitle", "Location", "StartDate" },
                values: new object[] { "Harte-Hanks Inc.", new DateOnly(2024, 3, 1), new[] { "Designed and developed a customer list system using .NET Core 5 and ASP.NET MVC with a PostgreSQL backend, hosted on a Linux server in AWS.", "Built CI/CD pipelines in Azure DevOps for deployment to Azure App Services.", "Supported, migrated, and enhanced automotive lead enrichment services in C# and VB.NET for BMW.", "Designed and developed a multi-channel messaging system for lead transmission to vendors using REST and SOAP with multiple authentication schemes.", "Designed, documented, and developed a Meta Graph API integration to post lead data to a REST endpoint.", "Designed and developed WebAPI and WCF services enabling external vendors to exchange data per STAR automotive specifications.", "Designed and developed internal web applications with 3-tier architecture supporting desktop and mobile layouts.", "Designed and developed an OAuth 2.0 SSO gateway to authenticate legacy applications.", "Developed a mobile web front-end repair system for Lenovo using HTML, JavaScript, and Bootstrap.", "Designed and developed SSIS packages for ETL and file processing.", "Analyzed and optimized database stored procedures to improve response time and reduce processing overhead.", "Migrated MongoDB data to a CentOS server on AWS.", "Designed and developed a dynamic SQL rules engine and internal web application enabling non-developers to configure the lead system.", "Migrated legacy applications to AWS, including IIS and SQL Server setup and database backup/restore operations.", "Created architecture and design documents for new websites, services and feature enhancements.", "Led a team's transition from TFS to Git.", "Led projects coordinating offshore developers, breaking down tasks to enable parallel workstreams.", "Estimated development scope and labor time for projects ranging between 40 and 500 hours." }, "Software Engineer III (Remote)", null, new DateOnly(2019, 10, 1) });

            migrationBuilder.InsertData(
                table: "Experience",
                columns: new[] { "Id", "Company", "EndDate", "Highlights", "JobTitle", "Location", "StartDate" },
                values: new object[,]
                {
                    { new Guid("33333333-3333-3333-3333-333333333303"), "Allied Electronics & Automation", new DateOnly(2019, 9, 1), new[] { "Developed and maintained a large-scale eCommerce website built with MVC 5, Web Forms, and TypeScript.", "Designed and developed responsive layouts using Foundation.", "Designed, documented, and developed REST API microservices wrapping search engine functionality and product information.", "Created UML use case and sequence diagrams for new system designs and planning.", "Created and maintained CI/CD release pipelines.", "Diagnosed and remedied network issues across Windows servers, F5 load balancers and security gateways.", "Administered IIS and configured new internal websites.", "Trained junior developers on clean architecture and SOLID principles.", "Participated in off-hours system monitoring rotation and critical system maintenance." }, "Web Developer", null, new DateOnly(2017, 12, 1) },
                    { new Guid("33333333-3333-3333-3333-333333333304"), "Harte-Hanks Inc.", new DateOnly(2017, 10, 1), new string[0], "Software Engineer III (Remote)", null, new DateOnly(2012, 4, 1) },
                    { new Guid("33333333-3333-3333-3333-333333333305"), "TekSystems (Contract Employee for BNSF Railroad)", new DateOnly(2012, 1, 1), new[] { "Designed and developed multi-threaded, scalable Windows services to process custom network communication messages for distributed systems, primarily over TCP/IP using clear, symmetric, and asymmetric cryptography schemes.", "Designed and developed service for securely transferring file data to railroad devices according to third-party specifications.", "Designed and developed services to transfer messages using AMQP technology and route messages from those services to simulate an internal network.", "Designed and developed device simulator for vehicles to communicate with back office over TCP/IP or RS-232 connection.", "Designed and developed C++/CLI modules and wrappers using mixed mode to interface with a hardware cryptographic module." }, ".NET Developer (details limited due to NDA)", null, new DateOnly(2011, 6, 1) },
                    { new Guid("33333333-3333-3333-3333-333333333306"), "Stryker Communications", new DateOnly(2011, 4, 1), new[] { "Developed and supported embedded firmware for lighting control panels.", "Developed test automation application for manufacturing to communicate over RS-232.", "Performed functional and environmental testing on medical devices.", "Created test protocols and reports for electronic and mechanical devices." }, "Sr. Lab Technician", null, new DateOnly(2010, 4, 1) },
                    { new Guid("33333333-3333-3333-3333-333333333307"), "GDSX Ltd.", new DateOnly(2008, 6, 1), new[] { "Designed, developed, and tested a data-bound Windows Forms application for retrieving, editing, and saving multiple field changes in a SQL Server database.", "Supported travel automation software integrating with Apollo, Sabre, and Worldspan GDS systems.", "Developed and maintained ASP.NET and JavaScript report templates using a SQL Server Reporting Services back end.", "Analyzed and coded maintenance fixes for a large-scale travel automation client." }, "Software Developer", null, new DateOnly(2007, 6, 1) }
                });

            migrationBuilder.UpdateData(
                table: "PersonalInfo",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "Email", "FullName", "GitHubUrl", "Headline", "LinkedInUrl", "Location", "Phone", "Summary", "WebsiteUrl" },
                values: new object[] { "jeff.m.proulx@gmail.com", "Jeffrey M. Proulx", null, "Lead/Senior Software Engineer", null, "Fort Worth, TX", "940-594-0410", "Full-stack software engineer with 15+ years of experience designing and modernizing distributed systems, REST APIs, and cloud infrastructure across AWS and Azure. Skilled at leading legacy application migrations, architecting vertical-slice services, and mentoring engineering teams.", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Education",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444402"));

            migrationBuilder.DeleteData(
                table: "Education",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444403"));

            migrationBuilder.DeleteData(
                table: "Experience",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333303"));

            migrationBuilder.DeleteData(
                table: "Experience",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333304"));

            migrationBuilder.DeleteData(
                table: "Experience",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333305"));

            migrationBuilder.DeleteData(
                table: "Experience",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333306"));

            migrationBuilder.DeleteData(
                table: "Experience",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333307"));

            migrationBuilder.DropColumn(
                name: "Location",
                table: "PersonalInfo");

            migrationBuilder.UpdateData(
                table: "Education",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444401"),
                columns: new[] { "Degree", "Details", "EndDate", "FieldOfStudy", "Institution", "StartDate" },
                values: new object[] { "B.S.", new[] { "Graduated cum laude", "Teaching assistant for Data Structures" }, new DateOnly(2018, 5, 1), "Computer Science", "State University", new DateOnly(2014, 9, 1) });

            migrationBuilder.UpdateData(
                table: "Experience",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333301"),
                columns: new[] { "Company", "Highlights", "JobTitle", "Location", "StartDate" },
                values: new object[] { "Northwind Traders", new[] { "Led migration of a monolithic ASP.NET application to a vertical-slice API architecture.", "Designed and shipped an internal developer platform used by 40+ engineers.", "Mentored 3 junior engineers through structured code review and pairing." }, "Senior Software Engineer", "Remote", new DateOnly(2022, 3, 1) });

            migrationBuilder.UpdateData(
                table: "Experience",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333302"),
                columns: new[] { "Company", "EndDate", "Highlights", "JobTitle", "Location", "StartDate" },
                values: new object[] { "Contoso Software", new DateOnly(2022, 2, 1), new[] { "Built and maintained a customer-facing React application serving 200k monthly users.", "Introduced automated integration testing, cutting production incidents by 30%." }, "Software Engineer", "Seattle, WA", new DateOnly(2018, 6, 1) });

            migrationBuilder.UpdateData(
                table: "PersonalInfo",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "Email", "FullName", "GitHubUrl", "Headline", "LinkedInUrl", "Phone", "Summary", "WebsiteUrl" },
                values: new object[] { "jordan.rivera@example.com", "Jordan Rivera", "https://github.com/jordanrivera", "Senior Software Engineer", "https://linkedin.com/in/jordanrivera", "555-0100", "Backend-leaning full-stack engineer with 10+ years building distributed systems, developer tooling, and web platforms.", "https://jordanrivera.dev" });

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
    }
}
