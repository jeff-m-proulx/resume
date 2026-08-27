using Resume.Data.Entities;

namespace Resume.Data;

public static class SeedData
{
    public static readonly Guid PersonalInfoId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static readonly PersonalInfo PersonalInfo = new()
    {
        Id = PersonalInfoId,
        FullName = "Jeffrey M. Proulx",
        Headline = "Lead/Senior Software Engineer",
        Location = "Fort Worth, TX",
        Email = "jeff.m.proulx@gmail.com",
        Phone = "940-594-0410",
        Summary = "Full-stack software engineer with 15+ years of experience designing and modernizing distributed systems, REST APIs, and cloud infrastructure across AWS and Azure. Skilled at leading legacy application migrations, architecting vertical-slice services, and mentoring engineering teams.",
        LinkedInUrl = null,
        GitHubUrl = null,
        WebsiteUrl = null
    };

    public static readonly Skill[] Skills = [];

    public static readonly Experience[] Experience =
    [
        new()
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333301"),
            Company = "AFG Companies",
            JobTitle = "Lead/Senior Software Engineer (Hybrid)",
            StartDate = new DateOnly(2024, 11, 1),
            EndDate = null,
            Highlights =
            [
                "Drove migration of a legacy web application from .NET Framework 4.8 to .NET 8 to completion.",
                "Supported and enhanced a legacy system to improve stability and performance.",
                "Supported applications using a mix of MVC, Angular, WebAPI, bootstrap, tailwind, and AWS services.",
                "Implemented repository, chain-of-responsibility, REPR endpoint, vertical slice, and rule engine design patterns to restructure processing logic.",
                "Optimized and refactored SQL Server stored procedures to eliminate redundant logic and improve maintainability.",
                "Consolidated and eliminated redundant API services to streamline processing.",
                "Wrote Terraform IaC to provision AWS resources, including deployment pipelines, Lambda functions, and ECR/ECS services.",
                "Configured Route 53 DNS routing rules to ELB instances.",
                "Designed and developed new REST API services using FastEndpoints, M2M Auth0 authentication, and a PostgreSQL backend."
            ]
        },
        new()
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333302"),
            Company = "Harte-Hanks Inc.",
            JobTitle = "Software Engineer III (Remote)",
            StartDate = new DateOnly(2019, 10, 1),
            EndDate = new DateOnly(2024, 3, 1),
            Highlights =
            [
                "Designed and developed a customer list system using .NET Core 5 and ASP.NET MVC with a PostgreSQL backend, hosted on a Linux server in AWS.",
                "Built CI/CD pipelines in Azure DevOps for deployment to Azure App Services.",
                "Supported, migrated, and enhanced automotive lead enrichment services in C# and VB.NET for BMW.",
                "Designed and developed a multi-channel messaging system for lead transmission to vendors using REST and SOAP with multiple authentication schemes.",
                "Designed, documented, and developed a Meta Graph API integration to post lead data to a REST endpoint.",
                "Designed and developed WebAPI and WCF services enabling external vendors to exchange data per STAR automotive specifications.",
                "Designed and developed internal web applications with 3-tier architecture supporting desktop and mobile layouts.",
                "Designed and developed an OAuth 2.0 SSO gateway to authenticate legacy applications.",
                "Developed a mobile web front-end repair system for Lenovo using HTML, JavaScript, and Bootstrap.",
                "Designed and developed SSIS packages for ETL and file processing.",
                "Analyzed and optimized database stored procedures to improve response time and reduce processing overhead.",
                "Migrated MongoDB data to a CentOS server on AWS.",
                "Designed and developed a dynamic SQL rules engine and internal web application enabling non-developers to configure the lead system.",
                "Migrated legacy applications to AWS, including IIS and SQL Server setup and database backup/restore operations.",
                "Created architecture and design documents for new websites, services and feature enhancements.",
                "Led a team's transition from TFS to Git.",
                "Led projects coordinating offshore developers, breaking down tasks to enable parallel workstreams.",
                "Estimated development scope and labor time for projects ranging between 40 and 500 hours."
            ]
        },
        new()
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333303"),
            Company = "Allied Electronics & Automation",
            JobTitle = "Web Developer",
            StartDate = new DateOnly(2017, 12, 1),
            EndDate = new DateOnly(2019, 9, 1),
            Highlights =
            [
                "Developed and maintained a large-scale eCommerce website built with MVC 5, Web Forms, and TypeScript.",
                "Designed and developed responsive layouts using Foundation.",
                "Designed, documented, and developed REST API microservices wrapping search engine functionality and product information.",
                "Created UML use case and sequence diagrams for new system designs and planning.",
                "Created and maintained CI/CD release pipelines.",
                "Diagnosed and remedied network issues across Windows servers, F5 load balancers and security gateways.",
                "Administered IIS and configured new internal websites.",
                "Trained junior developers on clean architecture and SOLID principles.",
                "Participated in off-hours system monitoring rotation and critical system maintenance."
            ]
        },
        new()
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333304"),
            Company = "Harte-Hanks Inc.",
            JobTitle = "Software Engineer III (Remote)",
            StartDate = new DateOnly(2012, 4, 1),
            EndDate = new DateOnly(2017, 10, 1),
            Highlights = []
        },
        new()
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333305"),
            Company = "TekSystems (Contract Employee for BNSF Railroad)",
            JobTitle = ".NET Developer (details limited due to NDA)",
            StartDate = new DateOnly(2011, 6, 1),
            EndDate = new DateOnly(2012, 1, 1),
            Highlights =
            [
                "Designed and developed multi-threaded, scalable Windows services to process custom network communication messages for distributed systems, primarily over TCP/IP using clear, symmetric, and asymmetric cryptography schemes.",
                "Designed and developed service for securely transferring file data to railroad devices according to third-party specifications.",
                "Designed and developed services to transfer messages using AMQP technology and route messages from those services to simulate an internal network.",
                "Designed and developed device simulator for vehicles to communicate with back office over TCP/IP or RS-232 connection.",
                "Designed and developed C++/CLI modules and wrappers using mixed mode to interface with a hardware cryptographic module."
            ]
        },
        new()
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333306"),
            Company = "Stryker Communications",
            JobTitle = "Sr. Lab Technician",
            StartDate = new DateOnly(2010, 4, 1),
            EndDate = new DateOnly(2011, 4, 1),
            Highlights =
            [
                "Developed and supported embedded firmware for lighting control panels.",
                "Developed test automation application for manufacturing to communicate over RS-232.",
                "Performed functional and environmental testing on medical devices.",
                "Created test protocols and reports for electronic and mechanical devices."
            ]
        },
        new()
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333307"),
            Company = "GDSX Ltd.",
            JobTitle = "Software Developer",
            StartDate = new DateOnly(2007, 6, 1),
            EndDate = new DateOnly(2008, 6, 1),
            Highlights =
            [
                "Designed, developed, and tested a data-bound Windows Forms application for retrieving, editing, and saving multiple field changes in a SQL Server database.",
                "Supported travel automation software integrating with Apollo, Sabre, and Worldspan GDS systems.",
                "Developed and maintained ASP.NET and JavaScript report templates using a SQL Server Reporting Services back end.",
                "Analyzed and coded maintenance fixes for a large-scale travel automation client."
            ]
        }
    ];

    public static readonly Education[] Education =
    [
        new()
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444401"),
            Institution = "DeVry University",
            Degree = "CIS",
            FieldOfStudy = "Database Administration",
            StartDate = new DateOnly(2010, 1, 1),
            EndDate = new DateOnly(2013, 1, 1),
            Details = []
        },
        new()
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444402"),
            Institution = "Coleman College",
            Degree = "CIS",
            FieldOfStudy = null,
            StartDate = new DateOnly(2004, 1, 1),
            EndDate = new DateOnly(2005, 1, 1),
            Details = []
        },
        new()
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444403"),
            Institution = "U.S. Marine Corps",
            Degree = "Telephone/Switchboard Repair Course, Basic Electronics Course, Fundamentals of Leadership Course",
            FieldOfStudy = null,
            StartDate = new DateOnly(1995, 1, 1),
            EndDate = new DateOnly(2000, 1, 1),
            Details = []
        }
    ];
}
