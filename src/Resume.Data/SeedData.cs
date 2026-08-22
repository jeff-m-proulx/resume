using Resume.Data.Entities;

namespace Resume.Data;

public static class SeedData
{
    public static readonly Guid PersonalInfoId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static readonly PersonalInfo PersonalInfo = new()
    {
        Id = PersonalInfoId,
        FullName = "Jordan Rivera",
        Headline = "Senior Software Engineer",
        Email = "jordan.rivera@example.com",
        Phone = "555-0100",
        Summary = "Backend-leaning full-stack engineer with 10+ years building distributed systems, developer tooling, and web platforms.",
        LinkedInUrl = "https://linkedin.com/in/jordanrivera",
        GitHubUrl = "https://github.com/jordanrivera",
        WebsiteUrl = "https://jordanrivera.dev"
    };

    public static readonly Skill[] Skills =
    [
        new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222201"), Category = "Languages", Name = "C#", SortOrder = 1 },
        new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222202"), Category = "Languages", Name = "TypeScript", SortOrder = 2 },
        new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222203"), Category = "Languages", Name = "SQL", SortOrder = 3 },
        new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222204"), Category = "Frameworks", Name = "ASP.NET Core", SortOrder = 1 },
        new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222205"), Category = "Frameworks", Name = "React", SortOrder = 2 },
        new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222206"), Category = "Frameworks", Name = "Blazor", SortOrder = 3 },
        new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222207"), Category = "Tools", Name = "Docker", SortOrder = 1 },
        new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222208"), Category = "Tools", Name = "Git", SortOrder = 2 },
        new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222209"), Category = "Cloud", Name = "Azure", SortOrder = 1 },
        new() { Id = Guid.Parse("22222222-2222-2222-2222-22222222220a"), Category = "Cloud", Name = "AWS", SortOrder = 2 }
    ];

    public static readonly Experience[] Experience =
    [
        new()
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333301"),
            Company = "Northwind Traders",
            JobTitle = "Senior Software Engineer",
            Location = "Remote",
            StartDate = new DateOnly(2022, 3, 1),
            EndDate = null,
            Highlights =
            [
                "Led migration of a monolithic ASP.NET application to a vertical-slice API architecture.",
                "Designed and shipped an internal developer platform used by 40+ engineers.",
                "Mentored 3 junior engineers through structured code review and pairing."
            ]
        },
        new()
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333302"),
            Company = "Contoso Software",
            JobTitle = "Software Engineer",
            Location = "Seattle, WA",
            StartDate = new DateOnly(2018, 6, 1),
            EndDate = new DateOnly(2022, 2, 1),
            Highlights =
            [
                "Built and maintained a customer-facing React application serving 200k monthly users.",
                "Introduced automated integration testing, cutting production incidents by 30%."
            ]
        }
    ];

    public static readonly Education[] Education =
    [
        new()
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444401"),
            Institution = "State University",
            Degree = "B.S.",
            FieldOfStudy = "Computer Science",
            StartDate = new DateOnly(2014, 9, 1),
            EndDate = new DateOnly(2018, 5, 1),
            Details = ["Graduated cum laude", "Teaching assistant for Data Structures"]
        }
    ];
}
