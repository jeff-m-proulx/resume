# Resume Web Application Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a .NET 10 resume web application with dual frontends (Blazor Web App, React) backed by a single FastEndpoints vertical-slice API and a containerized Postgres database, all orchestrated by .NET Aspire, where write endpoints validate but never persist.

**Architecture:** `Resume.Data` (EF Core entities + seed) is consumed only by `Resume.Api` (FastEndpoints, vertical slices per resume section). `Resume.Contracts` holds the shared DTOs consumed by `Resume.Api` and, via project reference, by `Resume.BlazorApp`. `Resume.BlazorApp` (Blazor Web App, Auto render mode) reaches the API two ways: server-side code calls it directly via Aspire service discovery, while its WebAssembly client calls a same-origin `/api/*` path that the server proxies to the API via YARP (browsers running WASM can't resolve Aspire's service-discovery URIs). `Resume.React` is a separate-origin Vite SPA that calls the API directly over CORS, with the API's resolved URL injected by Aspire as a Vite env var. `Resume.AppHost` wires Postgres, the API, and both frontends together for local development.

**Tech Stack:** .NET 10, C# 13, FastEndpoints 8.3.0 + FluentValidation, EF Core 10 + Npgsql, PostgreSQL (containerized via Aspire), .NET Aspire 13.5.2, Blazor Web App (Auto/WASM), YARP 2.3.0, React 18 + TypeScript + Vite, xUnit.

## Global Constraints

- Target framework `net10.0` for every C# project; React project targets Node 22 / TypeScript, built with Vite.
- `PersonalInfo` has no address field of any kind.
- Write endpoints (`Create`/`Update`) run full FluentValidation, then **must never call `SaveChangesAsync`** — they build and return the response as if the write succeeded, and the database is left untouched. A following `GET` must always show the original data.
- Only `Create` and `Update` endpoints exist — no `Delete` endpoints. `PersonalInfo` gets only `Update` (it is a singleton — there is nothing to create).
- No authentication or authorization on any endpoint. "Admin Mode" is a client-side UI toggle only, in both frontends, with no login.
- After any successful demo write in either frontend, show this exact banner text: `Saved — but this is a demo; changes are not actually persisted.`
- The database is seeded with realistic placeholder resume content (not empty tables), applied via EF Core migration `HasData`.
- Testing scope for this plan: xUnit unit/integration-style tests for `Resume.Api`'s vertical slices only (via `WebApplicationFactory` + EF Core InMemory). No frontend tests, no Aspire-hosted integration tests.
- Pinned package versions (verified against nuget.org as current stable, compatible with `net10.0`, on 2026-08-22):
  - `Aspire.AppHost.Sdk`, `Aspire.Hosting.AppHost`, `Aspire.Hosting.PostgreSQL`, `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` → `13.5.2`
  - `Aspire.Hosting.NodeJs` → `9.5.2`
  - `Aspire.ProjectTemplates` → `13.5.2`
  - `Microsoft.Extensions.ServiceDiscovery.Yarp` → `10.9.0`
  - `Yarp.ReverseProxy` → `2.3.0`
  - `FastEndpoints`, `FastEndpoints.Swagger` → `8.3.0`
  - `Npgsql.EntityFrameworkCore.PostgreSQL` → `10.0.3`
  - `Microsoft.EntityFrameworkCore.Design`, `Microsoft.EntityFrameworkCore.InMemory`, `Microsoft.AspNetCore.Mvc.Testing` → `10.0.11`
  - `dotnet-ef` global tool → `10.0.11`

---

## File Structure

```
Resume.sln
src/
  Resume.AppHost/                         Aspire orchestrator
  Resume.ServiceDefaults/                 Aspire shared telemetry/health/resilience
  Resume.Contracts/                       Shared DTOs (records)
    PersonalInfoDtos.cs
    SkillDtos.cs
    ExperienceDtos.cs
    EducationDtos.cs
  Resume.Data/                            EF Core entities, DbContext, seed, migrations
    Entities/{PersonalInfo,Skill,Experience,Education}.cs
    ResumeDbContext.cs
    SeedData.cs
    ResumeDbContextFactory.cs
    Migrations/...
  Resume.Api/                             FastEndpoints backend
    Program.cs
    Features/PersonalInfo/{GetPersonalInfoEndpoint,UpdatePersonalInfoEndpoint,UpdatePersonalInfoValidator}.cs
    Features/Skills/{GetSkillsEndpoint,CreateSkillEndpoint,CreateSkillValidator,UpdateSkillEndpoint,UpdateSkillValidator}.cs
    Features/Experience/{GetExperienceEndpoint,CreateExperienceEndpoint,CreateExperienceValidator,UpdateExperienceEndpoint,UpdateExperienceValidator}.cs
    Features/Education/{GetEducationEndpoint,CreateEducationEndpoint,CreateEducationValidator,UpdateEducationEndpoint,UpdateEducationValidator}.cs
  Resume.BlazorApp/                       Blazor Web App server/host project
    Program.cs
    Components/{App.razor,Routes.razor,_Imports.razor,Layout/MainLayout.razor}
    wwwroot/app.css
  Resume.BlazorApp.Client/                Blazor WASM client project (interactive components live here)
    Program.cs
    Services/ResumeApiClient.cs
    Pages/ResumePage.razor
    Components/ResumeSection.razor
    Components/{PersonalInfoForm,SkillForm,ExperienceForm,EducationForm}.razor
  Resume.React/                           Vite + React + TypeScript SPA
    package.json, vite.config.ts, index.html, tsconfig.json
    src/main.tsx, src/App.tsx, src/types.ts
    src/api/client.ts
    src/components/ResumeSection.tsx
    src/components/{PersonalInfoForm,SkillForm,ExperienceForm,EducationForm}.tsx
    src/index.css
tests/
  Resume.Api.Tests/
    ApiTestFixture.cs
    DataSeedTests.cs
    PersonalInfoEndpointTests.cs
    SkillsEndpointTests.cs
    ExperienceEndpointTests.cs
    EducationEndpointTests.cs
docs/superpowers/specs/2026-08-22-resume-app-design.md   (already committed)
```

---

### Task 1: Solution scaffolding, Aspire AppHost, and Postgres resource

**Files:**
- Create: `Resume.sln`
- Create: `src/Resume.AppHost/Resume.AppHost.csproj`, `src/Resume.AppHost/Program.cs`, `src/Resume.AppHost/appsettings.json`
- Create: `src/Resume.ServiceDefaults/Resume.ServiceDefaults.csproj`, `src/Resume.ServiceDefaults/Extensions.cs`

**Interfaces:**
- Produces: An Aspire `postgres` resource with a `resumedb` database, referenced by resource-builder variable name `resumedb` in `Resume.AppHost/Program.cs`, for later tasks to add `.WithReference(resumedb)` on the API project.
- Produces: `Resume.ServiceDefaults`'s `AddServiceDefaults()` and `MapDefaultEndpoints()` extension methods (generated by the Aspire template), consumed by `Resume.Api` and `Resume.BlazorApp` in later tasks.

- [ ] **Step 1: Install the Aspire project templates and scaffold the solution + AppHost + ServiceDefaults**

```bash
dotnet new install Aspire.ProjectTemplates::13.5.2
dotnet new sln -n Resume
dotnet new aspire-apphost -n Resume.AppHost -o src/Resume.AppHost
dotnet new aspire-servicedefaults -n Resume.ServiceDefaults -o src/Resume.ServiceDefaults
dotnet sln Resume.sln add src/Resume.AppHost/Resume.AppHost.csproj src/Resume.ServiceDefaults/Resume.ServiceDefaults.csproj
```

- [ ] **Step 2: Add the Postgres hosting package to the AppHost**

```bash
dotnet add src/Resume.AppHost/Resume.AppHost.csproj package Aspire.Hosting.PostgreSQL --version 13.5.2
```

- [ ] **Step 3: Replace `src/Resume.AppHost/Program.cs` with the Postgres resource definition**

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var resumedb = postgres.AddDatabase("resumedb");

builder.Build().Run();
```

- [ ] **Step 4: Build the solution**

Run: `dotnet build Resume.sln`
Expected: Build succeeds with 0 errors.

- [ ] **Step 5: Verify the Postgres resource starts**

Run: `dotnet run --project src/Resume.AppHost` (requires Docker Desktop running), then open the Aspire dashboard URL printed in the console.
Expected: The dashboard shows a `postgres` resource reaching the "Running" state. Stop the AppHost (Ctrl+C) once confirmed.

- [ ] **Step 6: Commit**

```bash
git add Resume.sln src/Resume.AppHost src/Resume.ServiceDefaults
git commit -m "Scaffold solution with Aspire AppHost and Postgres resource"
```

---

### Task 2: Resume.Data entities, DbContext, and seed data (TDD)

**Files:**
- Create: `src/Resume.Data/Resume.Data.csproj`
- Create: `src/Resume.Data/Entities/PersonalInfo.cs`, `src/Resume.Data/Entities/Skill.cs`, `src/Resume.Data/Entities/Experience.cs`, `src/Resume.Data/Entities/Education.cs`
- Create: `src/Resume.Data/ResumeDbContext.cs`
- Create: `src/Resume.Data/ResumeDbContextFactory.cs`
- Create: `tests/Resume.Api.Tests/Resume.Api.Tests.csproj`
- Create: `tests/Resume.Api.Tests/DataSeedTests.cs`
- Create (Step 4 only): `src/Resume.Data/SeedData.cs`

**Interfaces:**
- Produces: `Resume.Data.Entities.PersonalInfo { Guid Id, string FullName, string Headline, string Email, string? Phone, string Summary, string? LinkedInUrl, string? GitHubUrl, string? WebsiteUrl }`
- Produces: `Resume.Data.Entities.Skill { Guid Id, string Category, string Name, int SortOrder }`
- Produces: `Resume.Data.Entities.Experience { Guid Id, string Company, string JobTitle, string? Location, DateOnly StartDate, DateOnly? EndDate, string[] Highlights }`
- Produces: `Resume.Data.Entities.Education { Guid Id, string Institution, string Degree, string? FieldOfStudy, DateOnly StartDate, DateOnly? EndDate, string[] Details }`
- Produces: `Resume.Data.ResumeDbContext(DbContextOptions<ResumeDbContext>)` with `DbSet<PersonalInfo> PersonalInfo`, `DbSet<Skill> Skills`, `DbSet<Experience> Experience`, `DbSet<Education> Education`.
- Produces: `Resume.Data.SeedData` static class with `Guid PersonalInfoId`, `PersonalInfo PersonalInfo`, `Skill[] Skills`, `Experience[] Experience`, `Education[] Education` — consumed by tests in this task and by `Resume.Api.Tests` in later tasks.

- [ ] **Step 1: Create the `Resume.Data` class library and add it to the solution**

```bash
dotnet new classlib -n Resume.Data -o src/Resume.Data
dotnet sln Resume.sln add src/Resume.Data/Resume.Data.csproj
```

Replace `src/Resume.Data/Resume.Data.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.3" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.11">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

Delete the template-generated `src/Resume.Data/Class1.cs`.

- [ ] **Step 2: Add the entity classes**

`src/Resume.Data/Entities/PersonalInfo.cs`:
```csharp
namespace Resume.Data.Entities;

public class PersonalInfo
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public required string Headline { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public required string Summary { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? GitHubUrl { get; set; }
    public string? WebsiteUrl { get; set; }
}
```

`src/Resume.Data/Entities/Skill.cs`:
```csharp
namespace Resume.Data.Entities;

public class Skill
{
    public Guid Id { get; set; }
    public required string Category { get; set; }
    public required string Name { get; set; }
    public int SortOrder { get; set; }
}
```

`src/Resume.Data/Entities/Experience.cs`:
```csharp
namespace Resume.Data.Entities;

public class Experience
{
    public Guid Id { get; set; }
    public required string Company { get; set; }
    public required string JobTitle { get; set; }
    public string? Location { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string[] Highlights { get; set; } = [];
}
```

`src/Resume.Data/Entities/Education.cs`:
```csharp
namespace Resume.Data.Entities;

public class Education
{
    public Guid Id { get; set; }
    public required string Institution { get; set; }
    public required string Degree { get; set; }
    public string? FieldOfStudy { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string[] Details { get; set; } = [];
}
```

- [ ] **Step 3: Add `ResumeDbContext` (no seed data yet) and the design-time factory**

`src/Resume.Data/ResumeDbContext.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Resume.Data.Entities;

namespace Resume.Data;

public class ResumeDbContext(DbContextOptions<ResumeDbContext> options) : DbContext(options)
{
    public DbSet<PersonalInfo> PersonalInfo => Set<PersonalInfo>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<Experience> Experience => Set<Experience>();
    public DbSet<Education> Education => Set<Education>();
}
```

`src/Resume.Data/ResumeDbContextFactory.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Resume.Data;

public class ResumeDbContextFactory : IDesignTimeDbContextFactory<ResumeDbContext>
{
    public ResumeDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ResumeDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=resumedb;Username=postgres;Password=postgres");
        return new ResumeDbContext(optionsBuilder.Options);
    }
}
```

- [ ] **Step 4: Create the test project and write the failing seed tests**

```bash
dotnet new xunit -n Resume.Api.Tests -o tests/Resume.Api.Tests
dotnet sln Resume.sln add tests/Resume.Api.Tests/Resume.Api.Tests.csproj
dotnet add tests/Resume.Api.Tests/Resume.Api.Tests.csproj reference src/Resume.Data/Resume.Data.csproj
dotnet add tests/Resume.Api.Tests/Resume.Api.Tests.csproj package Microsoft.EntityFrameworkCore.InMemory --version 10.0.11
```

Delete the template-generated `tests/Resume.Api.Tests/UnitTest1.cs`.

`tests/Resume.Api.Tests/DataSeedTests.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Resume.Data;
using Xunit;

namespace Resume.Api.Tests;

public class DataSeedTests
{
    private static ResumeDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ResumeDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var context = new ResumeDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public void Seeded_database_contains_one_personal_info_record()
    {
        using var context = CreateInMemoryContext(nameof(Seeded_database_contains_one_personal_info_record));

        var personalInfo = Assert.Single(context.PersonalInfo);
        Assert.Equal(SeedData.PersonalInfoId, personalInfo.Id);
        Assert.Equal("Jordan Rivera", personalInfo.FullName);
    }

    [Fact]
    public void Seeded_database_contains_ten_skills_across_four_categories()
    {
        using var context = CreateInMemoryContext(nameof(Seeded_database_contains_ten_skills_across_four_categories));

        var skills = context.Skills.ToList();
        Assert.Equal(10, skills.Count);
        Assert.Equal(4, skills.Select(s => s.Category).Distinct().Count());
    }

    [Fact]
    public void Seeded_database_contains_two_experience_entries_with_highlights()
    {
        using var context = CreateInMemoryContext(nameof(Seeded_database_contains_two_experience_entries_with_highlights));

        var experience = context.Experience.ToList();
        Assert.Equal(2, experience.Count);
        Assert.All(experience, e => Assert.NotEmpty(e.Highlights));
    }

    [Fact]
    public void Seeded_database_contains_one_education_entry()
    {
        using var context = CreateInMemoryContext(nameof(Seeded_database_contains_one_education_entry));

        var education = Assert.Single(context.Education);
        Assert.Equal("State University", education.Institution);
    }
}
```

- [ ] **Step 5: Run the tests and verify they fail**

Run: `dotnet test tests/Resume.Api.Tests`
Expected: Build fails (`SeedData` does not exist) or, if you stub compilation by removing the `SeedData` reference temporarily, all 4 tests FAIL with 0 seeded rows. The key point: tests must not pass yet.

- [ ] **Step 6: Add `SeedData` and wire it into `OnModelCreating` via `HasData`**

`src/Resume.Data/SeedData.cs`:
```csharp
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
```

Modify `src/Resume.Data/ResumeDbContext.cs` to add:
```csharp
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PersonalInfo>().HasData(SeedData.PersonalInfo);
        modelBuilder.Entity<Skill>().HasData(SeedData.Skills);
        modelBuilder.Entity<Experience>().HasData(SeedData.Experience);
        modelBuilder.Entity<Education>().HasData(SeedData.Education);
    }
```
(placed inside the `ResumeDbContext` class body, after the `DbSet` properties)

- [ ] **Step 7: Run the tests and verify they pass**

Run: `dotnet test tests/Resume.Api.Tests`
Expected: All 4 tests PASS.

- [ ] **Step 8: Commit**

```bash
git add src/Resume.Data tests/Resume.Api.Tests Resume.sln
git commit -m "Add Resume.Data entities, DbContext, and seed data with tests"
```

---

### Task 3: EF Core migration targeting Npgsql

**Files:**
- Create: `src/Resume.Data/Migrations/*` (generated)

**Interfaces:**
- Consumes: `Resume.Data.ResumeDbContextFactory` (Task 2) for design-time migration generation.
- Produces: An `InitialCreate` migration that creates the `PersonalInfo`, `Skill`, `Experience`, `Education` tables (with `Highlights`/`Details` as native Postgres `text[]` columns) and inserts the `HasData` seed rows — applied by `Resume.Api` at startup in Task 8.

- [ ] **Step 1: Install/update the `dotnet-ef` global tool**

```bash
dotnet tool update -g dotnet-ef --version 10.0.11
```

- [ ] **Step 2: Generate the initial migration**

```bash
dotnet ef migrations add InitialCreate --project src/Resume.Data --startup-project src/Resume.Data
```

Expected: `src/Resume.Data/Migrations/` is created with `<timestamp>_InitialCreate.cs`, `<timestamp>_InitialCreate.Designer.cs`, and `ResumeDbContextModelSnapshot.cs`.

- [ ] **Step 3: Verify the migration's `Up()` method creates array columns and seeds data**

Open `src/Resume.Data/Migrations/<timestamp>_InitialCreate.cs` and confirm:
- `migrationBuilder.CreateTable(name: "Experience", ...)` includes a column `Highlights` of type `text[]`.
- `migrationBuilder.CreateTable(name: "Education", ...)` includes a column `Details` of type `text[]`.
- The file contains `migrationBuilder.InsertData(...)` calls for all four tables.

If any of these are missing, re-run Step 2 after confirming `OnModelCreating`'s `HasData` calls from Task 2 are present.

- [ ] **Step 4: Build to confirm the migration compiles**

Run: `dotnet build src/Resume.Data/Resume.Data.csproj`
Expected: Build succeeds with 0 errors.

- [ ] **Step 5: Commit**

```bash
git add src/Resume.Data/Migrations
git commit -m "Add InitialCreate EF Core migration for Resume.Data"
```

---

### Task 4: Resume.Contracts, Resume.Api scaffold, and the PersonalInfo slice

**Files:**
- Create: `src/Resume.Contracts/Resume.Contracts.csproj`, `src/Resume.Contracts/PersonalInfoDtos.cs`
- Create: `src/Resume.Api/Resume.Api.csproj`, `src/Resume.Api/Program.cs`, `src/Resume.Api/appsettings.json`
- Create: `src/Resume.Api/Features/PersonalInfo/GetPersonalInfoEndpoint.cs`, `src/Resume.Api/Features/PersonalInfo/UpdatePersonalInfoEndpoint.cs`
- Create: `tests/Resume.Api.Tests/ApiTestFixture.cs`, `tests/Resume.Api.Tests/PersonalInfoEndpointTests.cs`

**Interfaces:**
- Produces: `Resume.Contracts.PersonalInfoResponse(Guid Id, string FullName, string Headline, string Email, string? Phone, string Summary, string? LinkedInUrl, string? GitHubUrl, string? WebsiteUrl)`
- Produces: `Resume.Contracts.UpdatePersonalInfoRequest(string FullName, string Headline, string Email, string? Phone, string Summary, string? LinkedInUrl, string? GitHubUrl, string? WebsiteUrl)`
- Produces: `GET /api/personal-info` → `PersonalInfoResponse`; `PUT /api/personal-info` → `PersonalInfoResponse` (fake write).
- Produces: `Resume.Api.Tests.ApiTestFixture : WebApplicationFactory<Program>` — reused by every later endpoint test task; swaps `ResumeDbContext` to EF Core InMemory.

- [ ] **Step 1: Create `Resume.Contracts` and add the PersonalInfo DTOs**

```bash
dotnet new classlib -n Resume.Contracts -o src/Resume.Contracts
dotnet sln Resume.sln add src/Resume.Contracts/Resume.Contracts.csproj
```

Replace `src/Resume.Contracts/Resume.Contracts.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

</Project>
```

Delete `src/Resume.Contracts/Class1.cs`.

`src/Resume.Contracts/PersonalInfoDtos.cs`:
```csharp
namespace Resume.Contracts;

public record PersonalInfoResponse(
    Guid Id,
    string FullName,
    string Headline,
    string Email,
    string? Phone,
    string Summary,
    string? LinkedInUrl,
    string? GitHubUrl,
    string? WebsiteUrl);

public record UpdatePersonalInfoRequest(
    string FullName,
    string Headline,
    string Email,
    string? Phone,
    string Summary,
    string? LinkedInUrl,
    string? GitHubUrl,
    string? WebsiteUrl);
```

- [ ] **Step 2: Scaffold `Resume.Api`, add packages and project references**

```bash
dotnet new web -n Resume.Api -o src/Resume.Api
dotnet sln Resume.sln add src/Resume.Api/Resume.Api.csproj
dotnet add src/Resume.Api/Resume.Api.csproj reference src/Resume.Data/Resume.Data.csproj src/Resume.Contracts/Resume.Contracts.csproj src/Resume.ServiceDefaults/Resume.ServiceDefaults.csproj
dotnet add src/Resume.Api/Resume.Api.csproj package FastEndpoints --version 8.3.0
dotnet add src/Resume.Api/Resume.Api.csproj package FastEndpoints.Swagger --version 8.3.0
dotnet add src/Resume.Api/Resume.Api.csproj package Aspire.Npgsql.EntityFrameworkCore.PostgreSQL --version 13.5.2
```

- [ ] **Step 3: Replace `src/Resume.Api/Program.cs`**

```csharp
using FastEndpoints;
using FastEndpoints.Swagger;
using Resume.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<ResumeDbContext>("resumedb");

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseCors();
app.UseFastEndpoints();
app.UseSwaggerGen();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ResumeDbContext>();
    db.Database.Migrate();
}

app.Run();

public partial class Program;
```

- [ ] **Step 4: Add the PersonalInfo feature slice**

`src/Resume.Api/Features/PersonalInfo/GetPersonalInfoEndpoint.cs`:
```csharp
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Resume.Contracts;
using Resume.Data;

namespace Resume.Api.Features.PersonalInfo;

public class GetPersonalInfoEndpoint(ResumeDbContext db) : EndpointWithoutRequest<PersonalInfoResponse>
{
    public override void Configure()
    {
        Get("/api/personal-info");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var entity = await db.PersonalInfo.SingleAsync(ct);

        await SendAsync(new PersonalInfoResponse(
            entity.Id,
            entity.FullName,
            entity.Headline,
            entity.Email,
            entity.Phone,
            entity.Summary,
            entity.LinkedInUrl,
            entity.GitHubUrl,
            entity.WebsiteUrl), cancellation: ct);
    }
}
```

`src/Resume.Api/Features/PersonalInfo/UpdatePersonalInfoEndpoint.cs`:
```csharp
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Resume.Contracts;
using Resume.Data;

namespace Resume.Api.Features.PersonalInfo;

public class UpdatePersonalInfoValidator : Validator<UpdatePersonalInfoRequest>
{
    public UpdatePersonalInfoValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Headline).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Summary).NotEmpty().MaximumLength(2000);
    }
}

public class UpdatePersonalInfoEndpoint(ResumeDbContext db) : Endpoint<UpdatePersonalInfoRequest, PersonalInfoResponse>
{
    public override void Configure()
    {
        Put("/api/personal-info");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdatePersonalInfoRequest req, CancellationToken ct)
    {
        // Intentionally not persisted: this demo API validates and echoes the
        // submitted data back but never calls SaveChangesAsync.
        var existing = await db.PersonalInfo.SingleAsync(ct);

        await SendAsync(new PersonalInfoResponse(
            existing.Id,
            req.FullName,
            req.Headline,
            req.Email,
            req.Phone,
            req.Summary,
            req.LinkedInUrl,
            req.GitHubUrl,
            req.WebsiteUrl), cancellation: ct);
    }
}
```

- [ ] **Step 5: Write the failing endpoint tests and the shared test fixture**

```bash
dotnet add tests/Resume.Api.Tests/Resume.Api.Tests.csproj reference src/Resume.Api/Resume.Api.csproj src/Resume.Contracts/Resume.Contracts.csproj
dotnet add tests/Resume.Api.Tests/Resume.Api.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing --version 10.0.11
```

`tests/Resume.Api.Tests/ApiTestFixture.cs`:
```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Resume.Data;

namespace Resume.Api.Tests;

public class ApiTestFixture : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:resumedb", "Host=localhost;Database=test;Username=test;Password=test");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ResumeDbContext>>();
            services.AddDbContext<ResumeDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        });
    }
}
```

`tests/Resume.Api.Tests/PersonalInfoEndpointTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using Resume.Contracts;
using Resume.Data;
using Xunit;

namespace Resume.Api.Tests;

public class PersonalInfoEndpointTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    [Fact]
    public async Task Get_returns_seeded_personal_info()
    {
        var client = fixture.CreateClient();

        var response = await client.GetFromJsonAsync<PersonalInfoResponse>("/api/personal-info");

        Assert.NotNull(response);
        Assert.Equal(SeedData.PersonalInfoId, response.Id);
        Assert.Equal("Jordan Rivera", response.FullName);
    }

    [Fact]
    public async Task Put_with_valid_data_returns_submitted_values_without_persisting()
    {
        var client = fixture.CreateClient();
        var request = new UpdatePersonalInfoRequest(
            FullName: "Changed Name",
            Headline: "Changed Headline",
            Email: "changed@example.com",
            Phone: "555-9999",
            Summary: "Changed summary.",
            LinkedInUrl: null,
            GitHubUrl: null,
            WebsiteUrl: null);

        var putResponse = await client.PutAsJsonAsync("/api/personal-info", request);
        var updated = await putResponse.Content.ReadFromJsonAsync<PersonalInfoResponse>();

        Assert.True(putResponse.IsSuccessStatusCode);
        Assert.Equal("Changed Name", updated!.FullName);

        var getAfter = await client.GetFromJsonAsync<PersonalInfoResponse>("/api/personal-info");
        Assert.Equal("Jordan Rivera", getAfter!.FullName);
    }

    [Fact]
    public async Task Put_with_missing_full_name_returns_validation_error()
    {
        var client = fixture.CreateClient();
        var request = new UpdatePersonalInfoRequest(
            FullName: "",
            Headline: "Headline",
            Email: "valid@example.com",
            Phone: null,
            Summary: "Summary.",
            LinkedInUrl: null,
            GitHubUrl: null,
            WebsiteUrl: null);

        var response = await client.PutAsJsonAsync("/api/personal-info", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

- [ ] **Step 6: Run the tests and verify they pass**

Run: `dotnet test tests/Resume.Api.Tests`
Expected: All 3 `PersonalInfoEndpointTests` PASS, plus the 4 `DataSeedTests` from Task 2 still PASS (7 total). (Note: unlike the later feature-slice tasks, this task's tests are written after the endpoint code because the test fixture itself — `ApiTestFixture` — depends on `Program.cs` already existing; Tasks 5-7 restore strict test-first ordering now that the fixture exists.)

- [ ] **Step 7: Commit**

```bash
git add src/Resume.Contracts src/Resume.Api tests/Resume.Api.Tests Resume.sln
git commit -m "Add Resume.Contracts, Resume.Api scaffold, and PersonalInfo slice"
```

---

### Task 5: Skills feature slice

**Files:**
- Create: `src/Resume.Contracts/SkillDtos.cs`
- Create: `src/Resume.Api/Features/Skills/GetSkillsEndpoint.cs`, `src/Resume.Api/Features/Skills/CreateSkillEndpoint.cs`, `src/Resume.Api/Features/Skills/UpdateSkillEndpoint.cs`
- Create: `tests/Resume.Api.Tests/SkillsEndpointTests.cs`

**Interfaces:**
- Consumes: `ApiTestFixture` (Task 4).
- Produces: `Resume.Contracts.SkillResponse(Guid Id, string Category, string Name, int SortOrder)`, `CreateSkillRequest(string Category, string Name, int SortOrder)`, `UpdateSkillRequest(Guid Id, string Category, string Name, int SortOrder)`.
- Produces: `GET /api/skills` → `List<SkillResponse>`; `POST /api/skills` → `SkillResponse`; `PUT /api/skills/{Id}` → `SkillResponse` (fake write).

- [ ] **Step 1: Add the Skill DTOs**

`src/Resume.Contracts/SkillDtos.cs`:
```csharp
namespace Resume.Contracts;

public record SkillResponse(Guid Id, string Category, string Name, int SortOrder);

public record CreateSkillRequest(string Category, string Name, int SortOrder);

public record UpdateSkillRequest(Guid Id, string Category, string Name, int SortOrder);
```

- [ ] **Step 2: Write the failing tests**

`tests/Resume.Api.Tests/SkillsEndpointTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using Resume.Contracts;
using Xunit;

namespace Resume.Api.Tests;

public class SkillsEndpointTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    [Fact]
    public async Task Get_returns_all_ten_seeded_skills()
    {
        var client = fixture.CreateClient();

        var skills = await client.GetFromJsonAsync<List<SkillResponse>>("/api/skills");

        Assert.NotNull(skills);
        Assert.Equal(10, skills.Count);
    }

    [Fact]
    public async Task Post_with_valid_data_returns_created_skill_without_persisting()
    {
        var client = fixture.CreateClient();
        var request = new CreateSkillRequest(Category: "Languages", Name: "Rust", SortOrder: 4);

        var response = await client.PostAsJsonAsync("/api/skills", request);
        var created = await response.Content.ReadFromJsonAsync<SkillResponse>();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("Rust", created!.Name);
        Assert.NotEqual(Guid.Empty, created.Id);

        var skillsAfter = await client.GetFromJsonAsync<List<SkillResponse>>("/api/skills");
        Assert.Equal(10, skillsAfter!.Count);
    }

    [Fact]
    public async Task Put_with_valid_data_returns_updated_skill_without_persisting()
    {
        var client = fixture.CreateClient();
        var request = new UpdateSkillRequest(SeedData.Skills[0].Id, Category: "Languages", Name: "C# (Updated)", SortOrder: 1);

        var response = await client.PutAsJsonAsync($"/api/skills/{request.Id}", request);
        var updated = await response.Content.ReadFromJsonAsync<SkillResponse>();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("C# (Updated)", updated!.Name);

        var skillsAfter = await client.GetFromJsonAsync<List<SkillResponse>>("/api/skills");
        Assert.Contains(skillsAfter!, s => s.Id == SeedData.Skills[0].Id && s.Name == "C#");
    }

    [Fact]
    public async Task Post_with_empty_name_returns_validation_error()
    {
        var client = fixture.CreateClient();
        var request = new CreateSkillRequest(Category: "Languages", Name: "", SortOrder: 1);

        var response = await client.PostAsJsonAsync("/api/skills", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

Add `using Resume.Data;` is not required here since `SeedData` lives in namespace `Resume.Data` — add `using Resume.Data;` at the top of the file alongside the others.

- [ ] **Step 3: Run tests and verify they fail to compile (endpoints don't exist yet)**

Run: `dotnet test tests/Resume.Api.Tests`
Expected: Build FAILS — no routes exist yet for `/api/skills`.

- [ ] **Step 4: Add the Skills endpoints**

`src/Resume.Api/Features/Skills/GetSkillsEndpoint.cs`:
```csharp
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Resume.Contracts;
using Resume.Data;

namespace Resume.Api.Features.Skills;

public class GetSkillsEndpoint(ResumeDbContext db) : EndpointWithoutRequest<List<SkillResponse>>
{
    public override void Configure()
    {
        Get("/api/skills");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var skills = await db.Skills
            .OrderBy(s => s.Category)
            .ThenBy(s => s.SortOrder)
            .Select(s => new SkillResponse(s.Id, s.Category, s.Name, s.SortOrder))
            .ToListAsync(ct);

        await SendAsync(skills, cancellation: ct);
    }
}
```

`src/Resume.Api/Features/Skills/CreateSkillEndpoint.cs`:
```csharp
using FastEndpoints;
using FluentValidation;
using Resume.Contracts;
using Resume.Data;

namespace Resume.Api.Features.Skills;

public class CreateSkillValidator : Validator<CreateSkillRequest>
{
    public CreateSkillValidator()
    {
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public class CreateSkillEndpoint(ResumeDbContext db) : Endpoint<CreateSkillRequest, SkillResponse>
{
    public override void Configure()
    {
        Post("/api/skills");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateSkillRequest req, CancellationToken ct)
    {
        // Intentionally not persisted: see UpdatePersonalInfoEndpoint.
        await SendAsync(new SkillResponse(Guid.NewGuid(), req.Category, req.Name, req.SortOrder), cancellation: ct);
    }
}
```

`src/Resume.Api/Features/Skills/UpdateSkillEndpoint.cs`:
```csharp
using FastEndpoints;
using FluentValidation;
using Resume.Contracts;
using Resume.Data;

namespace Resume.Api.Features.Skills;

public class UpdateSkillValidator : Validator<UpdateSkillRequest>
{
    public UpdateSkillValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public class UpdateSkillEndpoint(ResumeDbContext db) : Endpoint<UpdateSkillRequest, SkillResponse>
{
    public override void Configure()
    {
        Put("/api/skills/{Id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateSkillRequest req, CancellationToken ct)
    {
        // Intentionally not persisted: see UpdatePersonalInfoEndpoint.
        await SendAsync(new SkillResponse(req.Id, req.Category, req.Name, req.SortOrder), cancellation: ct);
    }
}
```

- [ ] **Step 5: Run the tests and verify they pass**

Run: `dotnet test tests/Resume.Api.Tests`
Expected: All 4 `SkillsEndpointTests` PASS (11 total across the project).

- [ ] **Step 6: Commit**

```bash
git add src/Resume.Contracts/SkillDtos.cs src/Resume.Api/Features/Skills tests/Resume.Api.Tests/SkillsEndpointTests.cs
git commit -m "Add Skills feature slice"
```

---

### Task 6: Experience feature slice

**Files:**
- Create: `src/Resume.Contracts/ExperienceDtos.cs`
- Create: `src/Resume.Api/Features/Experience/GetExperienceEndpoint.cs`, `src/Resume.Api/Features/Experience/CreateExperienceEndpoint.cs`, `src/Resume.Api/Features/Experience/UpdateExperienceEndpoint.cs`
- Create: `tests/Resume.Api.Tests/ExperienceEndpointTests.cs`

**Interfaces:**
- Consumes: `ApiTestFixture` (Task 4).
- Produces: `Resume.Contracts.ExperienceResponse(Guid Id, string Company, string JobTitle, string? Location, DateOnly StartDate, DateOnly? EndDate, string[] Highlights)`, `CreateExperienceRequest(string Company, string JobTitle, string? Location, DateOnly StartDate, DateOnly? EndDate, string[] Highlights)`, `UpdateExperienceRequest(Guid Id, string Company, string JobTitle, string? Location, DateOnly StartDate, DateOnly? EndDate, string[] Highlights)`.
- Produces: `GET /api/experience` → `List<ExperienceResponse>`; `POST /api/experience` → `ExperienceResponse`; `PUT /api/experience/{Id}` → `ExperienceResponse` (fake write).

- [ ] **Step 1: Add the Experience DTOs**

`src/Resume.Contracts/ExperienceDtos.cs`:
```csharp
namespace Resume.Contracts;

public record ExperienceResponse(
    Guid Id,
    string Company,
    string JobTitle,
    string? Location,
    DateOnly StartDate,
    DateOnly? EndDate,
    string[] Highlights);

public record CreateExperienceRequest(
    string Company,
    string JobTitle,
    string? Location,
    DateOnly StartDate,
    DateOnly? EndDate,
    string[] Highlights);

public record UpdateExperienceRequest(
    Guid Id,
    string Company,
    string JobTitle,
    string? Location,
    DateOnly StartDate,
    DateOnly? EndDate,
    string[] Highlights);
```

- [ ] **Step 2: Write the failing tests**

`tests/Resume.Api.Tests/ExperienceEndpointTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using Resume.Contracts;
using Resume.Data;
using Xunit;

namespace Resume.Api.Tests;

public class ExperienceEndpointTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    [Fact]
    public async Task Get_returns_both_seeded_experience_entries()
    {
        var client = fixture.CreateClient();

        var experience = await client.GetFromJsonAsync<List<ExperienceResponse>>("/api/experience");

        Assert.NotNull(experience);
        Assert.Equal(2, experience.Count);
    }

    [Fact]
    public async Task Post_with_valid_data_returns_created_entry_without_persisting()
    {
        var client = fixture.CreateClient();
        var request = new CreateExperienceRequest(
            Company: "Acme Corp",
            JobTitle: "Staff Engineer",
            Location: "Austin, TX",
            StartDate: new DateOnly(2024, 1, 1),
            EndDate: null,
            Highlights: ["Shipped a new feature."]);

        var response = await client.PostAsJsonAsync("/api/experience", request);
        var created = await response.Content.ReadFromJsonAsync<ExperienceResponse>();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("Acme Corp", created!.Company);

        var experienceAfter = await client.GetFromJsonAsync<List<ExperienceResponse>>("/api/experience");
        Assert.Equal(2, experienceAfter!.Count);
    }

    [Fact]
    public async Task Put_with_valid_data_returns_updated_entry_without_persisting()
    {
        var client = fixture.CreateClient();
        var seeded = SeedData.Experience[0];
        var request = new UpdateExperienceRequest(
            seeded.Id, Company: "Changed Co", JobTitle: seeded.JobTitle, Location: seeded.Location,
            StartDate: seeded.StartDate, EndDate: seeded.EndDate, Highlights: seeded.Highlights);

        var response = await client.PutAsJsonAsync($"/api/experience/{request.Id}", request);
        var updated = await response.Content.ReadFromJsonAsync<ExperienceResponse>();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("Changed Co", updated!.Company);

        var experienceAfter = await client.GetFromJsonAsync<List<ExperienceResponse>>("/api/experience");
        Assert.Contains(experienceAfter!, e => e.Id == seeded.Id && e.Company == seeded.Company);
    }

    [Fact]
    public async Task Post_with_end_date_before_start_date_returns_validation_error()
    {
        var client = fixture.CreateClient();
        var request = new CreateExperienceRequest(
            Company: "Acme Corp",
            JobTitle: "Staff Engineer",
            Location: null,
            StartDate: new DateOnly(2024, 1, 1),
            EndDate: new DateOnly(2023, 1, 1),
            Highlights: ["Something."]);

        var response = await client.PostAsJsonAsync("/api/experience", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

- [ ] **Step 3: Run tests and verify they fail to compile**

Run: `dotnet test tests/Resume.Api.Tests`
Expected: Build FAILS — no routes exist yet for `/api/experience`.

- [ ] **Step 4: Add the Experience endpoints**

`src/Resume.Api/Features/Experience/GetExperienceEndpoint.cs`:
```csharp
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Resume.Contracts;
using Resume.Data;

namespace Resume.Api.Features.Experience;

public class GetExperienceEndpoint(ResumeDbContext db) : EndpointWithoutRequest<List<ExperienceResponse>>
{
    public override void Configure()
    {
        Get("/api/experience");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var experience = await db.Experience
            .OrderByDescending(e => e.StartDate)
            .Select(e => new ExperienceResponse(e.Id, e.Company, e.JobTitle, e.Location, e.StartDate, e.EndDate, e.Highlights))
            .ToListAsync(ct);

        await SendAsync(experience, cancellation: ct);
    }
}
```

`src/Resume.Api/Features/Experience/CreateExperienceEndpoint.cs`:
```csharp
using FastEndpoints;
using FluentValidation;
using Resume.Contracts;
using Resume.Data;

namespace Resume.Api.Features.Experience;

public class CreateExperienceValidator : Validator<CreateExperienceRequest>
{
    public CreateExperienceValidator()
    {
        RuleFor(x => x.Company).NotEmpty().MaximumLength(200);
        RuleFor(x => x.JobTitle).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Highlights).Must(h => h.Length > 0).WithMessage("At least one highlight is required.");
        RuleFor(x => x.EndDate)
            .Must((req, endDate) => endDate is null || endDate >= req.StartDate)
            .WithMessage("EndDate must be on or after StartDate.");
    }
}

public class CreateExperienceEndpoint(ResumeDbContext db) : Endpoint<CreateExperienceRequest, ExperienceResponse>
{
    public override void Configure()
    {
        Post("/api/experience");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateExperienceRequest req, CancellationToken ct)
    {
        // Intentionally not persisted: see UpdatePersonalInfoEndpoint.
        await SendAsync(new ExperienceResponse(Guid.NewGuid(), req.Company, req.JobTitle, req.Location, req.StartDate, req.EndDate, req.Highlights), cancellation: ct);
    }
}
```

`src/Resume.Api/Features/Experience/UpdateExperienceEndpoint.cs`:
```csharp
using FastEndpoints;
using FluentValidation;
using Resume.Contracts;
using Resume.Data;

namespace Resume.Api.Features.Experience;

public class UpdateExperienceValidator : Validator<UpdateExperienceRequest>
{
    public UpdateExperienceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Company).NotEmpty().MaximumLength(200);
        RuleFor(x => x.JobTitle).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Highlights).Must(h => h.Length > 0).WithMessage("At least one highlight is required.");
        RuleFor(x => x.EndDate)
            .Must((req, endDate) => endDate is null || endDate >= req.StartDate)
            .WithMessage("EndDate must be on or after StartDate.");
    }
}

public class UpdateExperienceEndpoint(ResumeDbContext db) : Endpoint<UpdateExperienceRequest, ExperienceResponse>
{
    public override void Configure()
    {
        Put("/api/experience/{Id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateExperienceRequest req, CancellationToken ct)
    {
        // Intentionally not persisted: see UpdatePersonalInfoEndpoint.
        await SendAsync(new ExperienceResponse(req.Id, req.Company, req.JobTitle, req.Location, req.StartDate, req.EndDate, req.Highlights), cancellation: ct);
    }
}
```

- [ ] **Step 5: Run the tests and verify they pass**

Run: `dotnet test tests/Resume.Api.Tests`
Expected: All 4 `ExperienceEndpointTests` PASS (15 total across the project).

- [ ] **Step 6: Commit**

```bash
git add src/Resume.Contracts/ExperienceDtos.cs src/Resume.Api/Features/Experience tests/Resume.Api.Tests/ExperienceEndpointTests.cs
git commit -m "Add Experience feature slice"
```

---

### Task 7: Education feature slice

**Files:**
- Create: `src/Resume.Contracts/EducationDtos.cs`
- Create: `src/Resume.Api/Features/Education/GetEducationEndpoint.cs`, `src/Resume.Api/Features/Education/CreateEducationEndpoint.cs`, `src/Resume.Api/Features/Education/UpdateEducationEndpoint.cs`
- Create: `tests/Resume.Api.Tests/EducationEndpointTests.cs`

**Interfaces:**
- Consumes: `ApiTestFixture` (Task 4).
- Produces: `Resume.Contracts.EducationResponse(Guid Id, string Institution, string Degree, string? FieldOfStudy, DateOnly StartDate, DateOnly? EndDate, string[] Details)`, `CreateEducationRequest(string Institution, string Degree, string? FieldOfStudy, DateOnly StartDate, DateOnly? EndDate, string[] Details)`, `UpdateEducationRequest(Guid Id, string Institution, string Degree, string? FieldOfStudy, DateOnly StartDate, DateOnly? EndDate, string[] Details)`.
- Produces: `GET /api/education` → `List<EducationResponse>`; `POST /api/education` → `EducationResponse`; `PUT /api/education/{Id}` → `EducationResponse` (fake write).

- [ ] **Step 1: Add the Education DTOs**

`src/Resume.Contracts/EducationDtos.cs`:
```csharp
namespace Resume.Contracts;

public record EducationResponse(
    Guid Id,
    string Institution,
    string Degree,
    string? FieldOfStudy,
    DateOnly StartDate,
    DateOnly? EndDate,
    string[] Details);

public record CreateEducationRequest(
    string Institution,
    string Degree,
    string? FieldOfStudy,
    DateOnly StartDate,
    DateOnly? EndDate,
    string[] Details);

public record UpdateEducationRequest(
    Guid Id,
    string Institution,
    string Degree,
    string? FieldOfStudy,
    DateOnly StartDate,
    DateOnly? EndDate,
    string[] Details);
```

- [ ] **Step 2: Write the failing tests**

`tests/Resume.Api.Tests/EducationEndpointTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using Resume.Contracts;
using Resume.Data;
using Xunit;

namespace Resume.Api.Tests;

public class EducationEndpointTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    [Fact]
    public async Task Get_returns_the_one_seeded_education_entry()
    {
        var client = fixture.CreateClient();

        var education = await client.GetFromJsonAsync<List<EducationResponse>>("/api/education");

        Assert.NotNull(education);
        Assert.Single(education);
    }

    [Fact]
    public async Task Post_with_valid_data_returns_created_entry_without_persisting()
    {
        var client = fixture.CreateClient();
        var request = new CreateEducationRequest(
            Institution: "Tech Institute",
            Degree: "M.S.",
            FieldOfStudy: "Software Engineering",
            StartDate: new DateOnly(2019, 9, 1),
            EndDate: new DateOnly(2021, 5, 1),
            Details: ["Thesis on distributed systems."]);

        var response = await client.PostAsJsonAsync("/api/education", request);
        var created = await response.Content.ReadFromJsonAsync<EducationResponse>();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("Tech Institute", created!.Institution);

        var educationAfter = await client.GetFromJsonAsync<List<EducationResponse>>("/api/education");
        Assert.Single(educationAfter!);
    }

    [Fact]
    public async Task Put_with_valid_data_returns_updated_entry_without_persisting()
    {
        var client = fixture.CreateClient();
        var seeded = SeedData.Education[0];
        var request = new UpdateEducationRequest(
            seeded.Id, Institution: "Changed University", Degree: seeded.Degree, FieldOfStudy: seeded.FieldOfStudy,
            StartDate: seeded.StartDate, EndDate: seeded.EndDate, Details: seeded.Details);

        var response = await client.PutAsJsonAsync($"/api/education/{request.Id}", request);
        var updated = await response.Content.ReadFromJsonAsync<EducationResponse>();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("Changed University", updated!.Institution);

        var educationAfter = await client.GetFromJsonAsync<List<EducationResponse>>("/api/education");
        Assert.Contains(educationAfter!, e => e.Id == seeded.Id && e.Institution == seeded.Institution);
    }

    [Fact]
    public async Task Post_with_missing_institution_returns_validation_error()
    {
        var client = fixture.CreateClient();
        var request = new CreateEducationRequest(
            Institution: "",
            Degree: "M.S.",
            FieldOfStudy: null,
            StartDate: new DateOnly(2019, 9, 1),
            EndDate: null,
            Details: []);

        var response = await client.PostAsJsonAsync("/api/education", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

- [ ] **Step 3: Run tests and verify they fail to compile**

Run: `dotnet test tests/Resume.Api.Tests`
Expected: Build FAILS — no routes exist yet for `/api/education`.

- [ ] **Step 4: Add the Education endpoints**

`src/Resume.Api/Features/Education/GetEducationEndpoint.cs`:
```csharp
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Resume.Contracts;
using Resume.Data;

namespace Resume.Api.Features.Education;

public class GetEducationEndpoint(ResumeDbContext db) : EndpointWithoutRequest<List<EducationResponse>>
{
    public override void Configure()
    {
        Get("/api/education");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var education = await db.Education
            .OrderByDescending(e => e.StartDate)
            .Select(e => new EducationResponse(e.Id, e.Institution, e.Degree, e.FieldOfStudy, e.StartDate, e.EndDate, e.Details))
            .ToListAsync(ct);

        await SendAsync(education, cancellation: ct);
    }
}
```

`src/Resume.Api/Features/Education/CreateEducationEndpoint.cs`:
```csharp
using FastEndpoints;
using FluentValidation;
using Resume.Contracts;
using Resume.Data;

namespace Resume.Api.Features.Education;

public class CreateEducationValidator : Validator<CreateEducationRequest>
{
    public CreateEducationValidator()
    {
        RuleFor(x => x.Institution).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Degree).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EndDate)
            .Must((req, endDate) => endDate is null || endDate >= req.StartDate)
            .WithMessage("EndDate must be on or after StartDate.");
    }
}

public class CreateEducationEndpoint(ResumeDbContext db) : Endpoint<CreateEducationRequest, EducationResponse>
{
    public override void Configure()
    {
        Post("/api/education");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateEducationRequest req, CancellationToken ct)
    {
        // Intentionally not persisted: see UpdatePersonalInfoEndpoint.
        await SendAsync(new EducationResponse(Guid.NewGuid(), req.Institution, req.Degree, req.FieldOfStudy, req.StartDate, req.EndDate, req.Details), cancellation: ct);
    }
}
```

`src/Resume.Api/Features/Education/UpdateEducationEndpoint.cs`:
```csharp
using FastEndpoints;
using FluentValidation;
using Resume.Contracts;
using Resume.Data;

namespace Resume.Api.Features.Education;

public class UpdateEducationValidator : Validator<UpdateEducationRequest>
{
    public UpdateEducationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Institution).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Degree).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EndDate)
            .Must((req, endDate) => endDate is null || endDate >= req.StartDate)
            .WithMessage("EndDate must be on or after StartDate.");
    }
}

public class UpdateEducationEndpoint(ResumeDbContext db) : Endpoint<UpdateEducationRequest, EducationResponse>
{
    public override void Configure()
    {
        Put("/api/education/{Id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateEducationRequest req, CancellationToken ct)
    {
        // Intentionally not persisted: see UpdatePersonalInfoEndpoint.
        await SendAsync(new EducationResponse(req.Id, req.Institution, req.Degree, req.FieldOfStudy, req.StartDate, req.EndDate, req.Details), cancellation: ct);
    }
}
```

- [ ] **Step 5: Run the tests and verify they pass**

Run: `dotnet test tests/Resume.Api.Tests`
Expected: All 4 `EducationEndpointTests` PASS (19 total across the project — all green).

- [ ] **Step 6: Commit**

```bash
git add src/Resume.Contracts/EducationDtos.cs src/Resume.Api/Features/Education tests/Resume.Api.Tests/EducationEndpointTests.cs
git commit -m "Add Education feature slice"
```

---

### Task 8: Wire Resume.Api into the AppHost

**Files:**
- Modify: `src/Resume.AppHost/Program.cs`

**Interfaces:**
- Consumes: `resumedb` resource-builder variable (Task 1), `Resume.Api` project (Task 4-7).
- Produces: An Aspire `api` resource-builder variable, consumed by `Resume.BlazorApp` (Task 9) and `Resume.React` (Task 12) wiring.

- [ ] **Step 1: Add the project reference and Postgres wiring in the AppHost**

```bash
dotnet add src/Resume.AppHost/Resume.AppHost.csproj reference src/Resume.Api/Resume.Api.csproj
```

Modify `src/Resume.AppHost/Program.cs`:
```csharp
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var resumedb = postgres.AddDatabase("resumedb");

var api = builder.AddProject<Projects.Resume_Api>("api")
    .WithReference(resumedb)
    .WaitFor(resumedb);

builder.Build().Run();
```

- [ ] **Step 2: Build the solution**

Run: `dotnet build Resume.sln`
Expected: Build succeeds with 0 errors.

- [ ] **Step 3: Run the AppHost and verify the API serves real seeded data from Postgres**

Run: `dotnet run --project src/Resume.AppHost` (requires Docker Desktop running)
Then, from the Aspire dashboard, find the `api` resource's endpoint URL and run:
```bash
curl https://localhost:<api-port>/api/personal-info
```
Expected: JSON response with `"fullName":"Jordan Rivera"`. Confirm the `postgres` and `api` resources both show "Running"/healthy in the dashboard. Stop the AppHost once confirmed.

- [ ] **Step 4: Commit**

```bash
git add src/Resume.AppHost
git commit -m "Wire Resume.Api and Postgres into the Aspire AppHost"
```

---

### Task 9: Resume.BlazorApp scaffold, YARP proxy, and smoke-tested API client

**Files:**
- Create: `src/Resume.BlazorApp/*`, `src/Resume.BlazorApp.Client/*` (via template)
- Modify: `src/Resume.BlazorApp/Program.cs`
- Create: `src/Resume.BlazorApp.Client/Services/ResumeApiClient.cs`
- Modify: `src/Resume.BlazorApp.Client/Program.cs`
- Modify: `src/Resume.BlazorApp/Components/Pages/Home.razor` (replaced by `ResumePage.razor` routing in Task 10 — this task adds a minimal smoke-test render)
- Modify: `src/Resume.AppHost/Program.cs`

**Interfaces:**
- Consumes: `Resume.Contracts` DTOs (Tasks 4-7), `api` resource-builder variable (Task 8).
- Produces: `Resume.BlazorApp.Client.Services.ResumeApiClient(HttpClient http)` with `GetPersonalInfoAsync()`, `UpdatePersonalInfoAsync(UpdatePersonalInfoRequest)`, `GetSkillsAsync()`, `CreateSkillAsync(CreateSkillRequest)`, `UpdateSkillAsync(UpdateSkillRequest)`, `GetExperienceAsync()`, `CreateExperienceAsync(CreateExperienceRequest)`, `UpdateExperienceAsync(UpdateExperienceRequest)`, `GetEducationAsync()`, `CreateEducationAsync(CreateEducationRequest)`, `UpdateEducationAsync(UpdateEducationRequest)` — consumed by `ResumePage.razor` in Tasks 10-11.

- [ ] **Step 1: Scaffold the Blazor Web App with Auto interactivity**

```bash
dotnet new blazor -n Resume.BlazorApp -o src/Resume.BlazorApp --interactivity Auto -ai -e
dotnet sln Resume.sln add src/Resume.BlazorApp/Resume.BlazorApp.csproj src/Resume.BlazorApp.Client/Resume.BlazorApp.Client.csproj
dotnet add src/Resume.BlazorApp/Resume.BlazorApp.csproj reference src/Resume.ServiceDefaults/Resume.ServiceDefaults.csproj src/Resume.BlazorApp.Client/Resume.BlazorApp.Client.csproj
dotnet add src/Resume.BlazorApp.Client/Resume.BlazorApp.Client.csproj reference src/Resume.Contracts/Resume.Contracts.csproj
dotnet add src/Resume.BlazorApp/Resume.BlazorApp.csproj package Yarp.ReverseProxy --version 2.3.0
dotnet add src/Resume.BlazorApp/Resume.BlazorApp.csproj package Microsoft.Extensions.ServiceDiscovery.Yarp --version 10.9.0
```

- [ ] **Step 2: Add `ResumeApiClient` to the Client (WASM) project**

`src/Resume.BlazorApp.Client/Services/ResumeApiClient.cs`:
```csharp
using System.Net.Http.Json;
using Resume.Contracts;

namespace Resume.BlazorApp.Client.Services;

public class ResumeApiClient(HttpClient http)
{
    public async Task<PersonalInfoResponse> GetPersonalInfoAsync() =>
        await http.GetFromJsonAsync<PersonalInfoResponse>("personal-info")
        ?? throw new InvalidOperationException("Personal info response was empty.");

    public async Task<PersonalInfoResponse> UpdatePersonalInfoAsync(UpdatePersonalInfoRequest request)
    {
        var response = await http.PutAsJsonAsync("personal-info", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PersonalInfoResponse>())!;
    }

    public async Task<List<SkillResponse>> GetSkillsAsync() =>
        await http.GetFromJsonAsync<List<SkillResponse>>("skills") ?? [];

    public async Task<SkillResponse> CreateSkillAsync(CreateSkillRequest request)
    {
        var response = await http.PostAsJsonAsync("skills", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SkillResponse>())!;
    }

    public async Task<SkillResponse> UpdateSkillAsync(UpdateSkillRequest request)
    {
        var response = await http.PutAsJsonAsync($"skills/{request.Id}", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SkillResponse>())!;
    }

    public async Task<List<ExperienceResponse>> GetExperienceAsync() =>
        await http.GetFromJsonAsync<List<ExperienceResponse>>("experience") ?? [];

    public async Task<ExperienceResponse> CreateExperienceAsync(CreateExperienceRequest request)
    {
        var response = await http.PostAsJsonAsync("experience", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ExperienceResponse>())!;
    }

    public async Task<ExperienceResponse> UpdateExperienceAsync(UpdateExperienceRequest request)
    {
        var response = await http.PutAsJsonAsync($"experience/{request.Id}", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ExperienceResponse>())!;
    }

    public async Task<List<EducationResponse>> GetEducationAsync() =>
        await http.GetFromJsonAsync<List<EducationResponse>>("education") ?? [];

    public async Task<EducationResponse> CreateEducationAsync(CreateEducationRequest request)
    {
        var response = await http.PostAsJsonAsync("education", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EducationResponse>())!;
    }

    public async Task<EducationResponse> UpdateEducationAsync(UpdateEducationRequest request)
    {
        var response = await http.PutAsJsonAsync($"education/{request.Id}", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EducationResponse>())!;
    }
}
```

- [ ] **Step 3: Register `ResumeApiClient` in the Client (WASM) project — same-origin `/api/` calls**

Replace `src/Resume.BlazorApp.Client/Program.cs`:
```csharp
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Resume.BlazorApp.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddHttpClient<ResumeApiClient>(client =>
    client.BaseAddress = new Uri($"{builder.HostEnvironment.BaseAddress}api/"));

await builder.Build().RunAsync();
```

- [ ] **Step 4: Register `ResumeApiClient` in the Server project (direct service discovery) and add the YARP proxy for the WASM path**

Replace `src/Resume.BlazorApp/Program.cs`, keeping the template's existing `app.UseAntiforgery()`/static-file/HTTPS lines and `App`/`Routes` component mapping, adding the pieces below:
```csharp
using Resume.BlazorApp.Client.Services;
using Resume.BlazorApp.Components;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddHttpClient<ResumeApiClient>(client =>
    client.BaseAddress = new Uri("https+http://api/api/"));

builder.Services.AddReverseProxy()
    .AddServiceDiscoveryDestinationResolver()
    .LoadFromMemory(
        [
            new RouteConfig
            {
                RouteId = "api",
                ClusterId = "api-cluster",
                Match = new RouteMatch { Path = "/api/{**catch-all}" }
            }
        ],
        [
            new ClusterConfig
            {
                ClusterId = "api-cluster",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["destination1"] = new DestinationConfig { Address = "https+http://api" }
                }
            }
        ]);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapDefaultEndpoints();
app.MapReverseProxy();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Resume.BlazorApp.Client._Imports).Assembly);

app.Run();
```

- [ ] **Step 5: Replace the template's sample home page with a smoke-test render**

Replace the contents of `src/Resume.BlazorApp/Components/Pages/Home.razor` with:
```razor
@page "/"
@rendermode InteractiveAuto
@inject ResumeApiClient Api

<PageTitle>Resume</PageTitle>

@if (_personalInfo is null)
{
    <p>Loading...</p>
}
else
{
    <h1>@_personalInfo.FullName</h1>
    <p>@_personalInfo.Headline</p>
}

@code {
    private Resume.Contracts.PersonalInfoResponse? _personalInfo;

    protected override async Task OnInitializedAsync() =>
        _personalInfo = await Api.GetPersonalInfoAsync();
}
```

Add `@using Resume.BlazorApp.Client.Services` to `src/Resume.BlazorApp/Components/_Imports.razor`.

- [ ] **Step 6: Wire `Resume.BlazorApp` into the AppHost**

```bash
dotnet add src/Resume.AppHost/Resume.AppHost.csproj reference src/Resume.BlazorApp/Resume.BlazorApp.csproj
```

Modify `src/Resume.AppHost/Program.cs`, adding after the `api` resource:
```csharp
var blazorApp = builder.AddProject<Projects.Resume_BlazorApp>("blazorapp")
    .WithReference(api)
    .WaitFor(api);
```
(and change the final line's ordering is unaffected — `builder.Build().Run();` stays last)

- [ ] **Step 7: Build and smoke-test**

Run: `dotnet build Resume.sln`
Expected: Build succeeds with 0 errors.

Run: `dotnet run --project src/Resume.AppHost` (Docker Desktop running), open the `blazorapp` resource's URL from the Aspire dashboard.
Expected: The page loads and displays "Jordan Rivera" / "Senior Software Engineer". Refresh once to confirm it still works after the WASM client takes over (Auto mode). Stop the AppHost once confirmed.

- [ ] **Step 8: Commit**

```bash
git add src/Resume.BlazorApp src/Resume.BlazorApp.Client src/Resume.AppHost Resume.sln
git commit -m "Add Resume.BlazorApp with YARP-proxied API access and Aspire wiring"
```

---

### Task 10: Blazor full resume UI (read-only)

**Files:**
- Create: `src/Resume.BlazorApp.Client/Components/ResumeSection.razor`
- Create: `src/Resume.BlazorApp.Client/Pages/ResumePage.razor`
- Modify: `src/Resume.BlazorApp/Components/Pages/Home.razor` (delete — replaced by routing through the Client's `ResumePage`)
- Modify: `src/Resume.BlazorApp/wwwroot/app.css`

**Interfaces:**
- Consumes: `ResumeApiClient` (Task 9).
- Produces: `ResumeSection` component with parameters `string Title`, `bool InitiallyExpanded`, `RenderFragment ChildContent` — consumed again by Task 11's admin forms.

- [ ] **Step 1: Delete the smoke-test Home page (routing now lives in the Client project)**

```bash
rm src/Resume.BlazorApp/Components/Pages/Home.razor
```

- [ ] **Step 2: Add the collapsible, sticky-header `ResumeSection` component**

`src/Resume.BlazorApp.Client/Components/ResumeSection.razor`:
```razor
<div class="resume-section">
    <button class="resume-section__header" @onclick="ToggleExpanded" aria-expanded="@_isExpanded">
        <span>@Title</span>
        <span class="resume-section__chevron">@(_isExpanded ? "▾" : "▸")</span>
    </button>
    @if (_isExpanded)
    {
        <div class="resume-section__content">
            @ChildContent
        </div>
    }
</div>

@code {
    [Parameter, EditorRequired] public string Title { get; set; } = "";
    [Parameter] public bool InitiallyExpanded { get; set; }
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    private bool _isExpanded;

    protected override void OnInitialized() => _isExpanded = InitiallyExpanded;

    private void ToggleExpanded() => _isExpanded = !_isExpanded;
}
```

- [ ] **Step 3: Add the sticky-header and section styling**

Append to `src/Resume.BlazorApp/wwwroot/app.css`:
```css
.resume-page {
    max-width: 800px;
    margin: 0 auto;
    padding: 1rem;
}

.resume-section {
    border: 1px solid #d0d7de;
    border-radius: 8px;
    margin-bottom: 1rem;
    overflow: hidden;
}

.resume-section__header {
    position: sticky;
    top: 0;
    z-index: 1;
    width: 100%;
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.75rem 1rem;
    background: #f6f8fa;
    border: none;
    border-bottom: 1px solid #d0d7de;
    font-size: 1.1rem;
    font-weight: 600;
    cursor: pointer;
    text-align: left;
}

.resume-section__content {
    padding: 1rem;
}

.resume-entry {
    margin-bottom: 1rem;
}

.resume-banner {
    position: sticky;
    bottom: 0;
    background: #fff8c5;
    border: 1px solid #d4a72c;
    border-radius: 6px;
    padding: 0.75rem 1rem;
    margin-top: 1rem;
}
```

- [ ] **Step 4: Add the composed resume page**

`src/Resume.BlazorApp.Client/Pages/ResumePage.razor`:
```razor
@page "/"
@rendermode InteractiveAuto
@using Resume.Contracts
@inject ResumeApiClient Api

<PageTitle>Resume</PageTitle>

<div class="resume-page">
    @if (_personalInfo is null)
    {
        <p>Loading...</p>
    }
    else
    {
        <ResumeSection Title="Personal Info" InitiallyExpanded="true">
            <p><strong>@_personalInfo.FullName</strong> — @_personalInfo.Headline</p>
            <p>@_personalInfo.Summary</p>
            <p>
                @_personalInfo.Email
                @if (_personalInfo.Phone is not null)
                {
                    <text> • @_personalInfo.Phone</text>
                }
            </p>
        </ResumeSection>

        <ResumeSection Title="Skills">
            @foreach (var group in _skills.GroupBy(s => s.Category))
            {
                <h4>@group.Key</h4>
                <ul>
                    @foreach (var skill in group.OrderBy(s => s.SortOrder))
                    {
                        <li>@skill.Name</li>
                    }
                </ul>
            }
        </ResumeSection>

        <ResumeSection Title="Experience">
            @foreach (var job in _experience)
            {
                <div class="resume-entry">
                    <h4>@job.JobTitle, @job.Company</h4>
                    <p>@job.StartDate.ToString("MMM yyyy") - @(job.EndDate?.ToString("MMM yyyy") ?? "Present")</p>
                    <ul>
                        @foreach (var highlight in job.Highlights)
                        {
                            <li>@highlight</li>
                        }
                    </ul>
                </div>
            }
        </ResumeSection>

        <ResumeSection Title="Education">
            @foreach (var edu in _education)
            {
                <div class="resume-entry">
                    <h4>@edu.Degree, @edu.Institution</h4>
                    <p>@edu.StartDate.ToString("MMM yyyy") - @(edu.EndDate?.ToString("MMM yyyy") ?? "Present")</p>
                </div>
            }
        </ResumeSection>
    }
</div>

@code {
    private PersonalInfoResponse? _personalInfo;
    private List<SkillResponse> _skills = [];
    private List<ExperienceResponse> _experience = [];
    private List<EducationResponse> _education = [];

    protected override async Task OnInitializedAsync() => await LoadAllAsync();

    private async Task LoadAllAsync()
    {
        _personalInfo = await Api.GetPersonalInfoAsync();
        _skills = await Api.GetSkillsAsync();
        _experience = await Api.GetExperienceAsync();
        _education = await Api.GetEducationAsync();
    }
}
```

- [ ] **Step 5: Build and manually verify**

Run: `dotnet build Resume.sln`
Expected: Build succeeds with 0 errors.

Run: `dotnet run --project src/Resume.AppHost`, open the `blazorapp` URL.
Expected: Personal Info is expanded by default; Skills/Experience/Education are collapsed and expand on click; clicking a section header while its content is long keeps the header pinned to the top while scrolling. Stop the AppHost once confirmed.

- [ ] **Step 6: Commit**

```bash
git add src/Resume.BlazorApp src/Resume.BlazorApp.Client
git commit -m "Add full read-only Blazor resume UI with collapsible sticky-header sections"
```

---

### Task 11: Blazor Admin Mode (create/edit forms)

**Files:**
- Create: `src/Resume.BlazorApp.Client/Components/PersonalInfoForm.razor`, `SkillForm.razor`, `ExperienceForm.razor`, `EducationForm.razor`
- Modify: `src/Resume.BlazorApp.Client/Pages/ResumePage.razor`

**Interfaces:**
- Consumes: `ResumeApiClient` (Task 9), `ResumeSection` (Task 10).

- [ ] **Step 1: Add the PersonalInfo edit form**

`src/Resume.BlazorApp.Client/Components/PersonalInfoForm.razor`:
```razor
@using Resume.Contracts
@inject ResumeApiClient Api

<EditForm Model="_model" OnValidSubmit="Submit">
    <div><label>Full name <InputText @bind-Value="_model.FullName" /></label></div>
    <div><label>Headline <InputText @bind-Value="_model.Headline" /></label></div>
    <div><label>Email <InputText @bind-Value="_model.Email" /></label></div>
    <div><label>Summary <InputTextArea @bind-Value="_model.Summary" /></label></div>
    <button type="submit">Save</button>
</EditForm>

@code {
    [Parameter, EditorRequired] public PersonalInfoResponse PersonalInfo { get; set; } = default!;
    [Parameter] public EventCallback OnSaved { get; set; }

    private FormModel _model = new();

    protected override void OnParametersSet() => _model = new FormModel
    {
        FullName = PersonalInfo.FullName,
        Headline = PersonalInfo.Headline,
        Email = PersonalInfo.Email,
        Summary = PersonalInfo.Summary
    };

    private async Task Submit()
    {
        await Api.UpdatePersonalInfoAsync(new UpdatePersonalInfoRequest(
            _model.FullName, _model.Headline, _model.Email, PersonalInfo.Phone, _model.Summary,
            PersonalInfo.LinkedInUrl, PersonalInfo.GitHubUrl, PersonalInfo.WebsiteUrl));
        await OnSaved.InvokeAsync();
    }

    private class FormModel
    {
        public string FullName { get; set; } = "";
        public string Headline { get; set; } = "";
        public string Email { get; set; } = "";
        public string Summary { get; set; } = "";
    }
}
```

- [ ] **Step 2: Add the Skill add/edit form**

`src/Resume.BlazorApp.Client/Components/SkillForm.razor`:
```razor
@using Resume.Contracts
@inject ResumeApiClient Api

<EditForm Model="_model" OnValidSubmit="Submit">
    <div><label>Category <InputText @bind-Value="_model.Category" /></label></div>
    <div><label>Name <InputText @bind-Value="_model.Name" /></label></div>
    <div><label>Sort order <InputNumber @bind-Value="_model.SortOrder" /></label></div>
    <button type="submit">@(EditingSkill is null ? "Add" : "Save")</button>
</EditForm>

@code {
    [Parameter] public SkillResponse? EditingSkill { get; set; }
    [Parameter] public EventCallback OnSaved { get; set; }

    private FormModel _model = new();

    protected override void OnParametersSet() => _model = EditingSkill is null
        ? new FormModel()
        : new FormModel { Category = EditingSkill.Category, Name = EditingSkill.Name, SortOrder = EditingSkill.SortOrder };

    private async Task Submit()
    {
        if (EditingSkill is null)
        {
            await Api.CreateSkillAsync(new CreateSkillRequest(_model.Category, _model.Name, _model.SortOrder));
        }
        else
        {
            await Api.UpdateSkillAsync(new UpdateSkillRequest(EditingSkill.Id, _model.Category, _model.Name, _model.SortOrder));
        }

        await OnSaved.InvokeAsync();
    }

    private class FormModel
    {
        public string Category { get; set; } = "";
        public string Name { get; set; } = "";
        public int SortOrder { get; set; }
    }
}
```

- [ ] **Step 3: Add the Experience add/edit form**

`src/Resume.BlazorApp.Client/Components/ExperienceForm.razor`:
```razor
@using Resume.Contracts
@inject ResumeApiClient Api

<EditForm Model="_model" OnValidSubmit="Submit">
    <div><label>Company <InputText @bind-Value="_model.Company" /></label></div>
    <div><label>Job title <InputText @bind-Value="_model.JobTitle" /></label></div>
    <div><label>Start date <InputDate @bind-Value="_model.StartDate" /></label></div>
    <div><label>Highlights (one per line) <InputTextArea @bind-Value="_model.HighlightsText" /></label></div>
    <button type="submit">@(EditingEntry is null ? "Add" : "Save")</button>
</EditForm>

@code {
    [Parameter] public ExperienceResponse? EditingEntry { get; set; }
    [Parameter] public EventCallback OnSaved { get; set; }

    private FormModel _model = new();

    protected override void OnParametersSet() => _model = EditingEntry is null
        ? new FormModel { StartDate = DateTime.Today }
        : new FormModel
        {
            Company = EditingEntry.Company,
            JobTitle = EditingEntry.JobTitle,
            StartDate = EditingEntry.StartDate.ToDateTime(TimeOnly.MinValue),
            HighlightsText = string.Join('\n', EditingEntry.Highlights)
        };

    private async Task Submit()
    {
        var startDate = DateOnly.FromDateTime(_model.StartDate);
        var highlights = _model.HighlightsText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (EditingEntry is null)
        {
            await Api.CreateExperienceAsync(new CreateExperienceRequest(_model.Company, _model.JobTitle, null, startDate, null, highlights));
        }
        else
        {
            await Api.UpdateExperienceAsync(new UpdateExperienceRequest(EditingEntry.Id, _model.Company, _model.JobTitle, EditingEntry.Location, startDate, EditingEntry.EndDate, highlights));
        }

        await OnSaved.InvokeAsync();
    }

    private class FormModel
    {
        public string Company { get; set; } = "";
        public string JobTitle { get; set; } = "";
        public DateTime StartDate { get; set; }
        public string HighlightsText { get; set; } = "";
    }
}
```

- [ ] **Step 4: Add the Education add/edit form**

`src/Resume.BlazorApp.Client/Components/EducationForm.razor`:
```razor
@using Resume.Contracts
@inject ResumeApiClient Api

<EditForm Model="_model" OnValidSubmit="Submit">
    <div><label>Institution <InputText @bind-Value="_model.Institution" /></label></div>
    <div><label>Degree <InputText @bind-Value="_model.Degree" /></label></div>
    <div><label>Start date <InputDate @bind-Value="_model.StartDate" /></label></div>
    <button type="submit">@(EditingEntry is null ? "Add" : "Save")</button>
</EditForm>

@code {
    [Parameter] public EducationResponse? EditingEntry { get; set; }
    [Parameter] public EventCallback OnSaved { get; set; }

    private FormModel _model = new();

    protected override void OnParametersSet() => _model = EditingEntry is null
        ? new FormModel { StartDate = DateTime.Today }
        : new FormModel
        {
            Institution = EditingEntry.Institution,
            Degree = EditingEntry.Degree,
            StartDate = EditingEntry.StartDate.ToDateTime(TimeOnly.MinValue)
        };

    private async Task Submit()
    {
        var startDate = DateOnly.FromDateTime(_model.StartDate);

        if (EditingEntry is null)
        {
            await Api.CreateEducationAsync(new CreateEducationRequest(_model.Institution, _model.Degree, null, startDate, null, []));
        }
        else
        {
            await Api.UpdateEducationAsync(new UpdateEducationRequest(EditingEntry.Id, _model.Institution, _model.Degree, EditingEntry.FieldOfStudy, startDate, EditingEntry.EndDate, EditingEntry.Details));
        }

        await OnSaved.InvokeAsync();
    }

    private class FormModel
    {
        public string Institution { get; set; } = "";
        public string Degree { get; set; } = "";
        public DateTime StartDate { get; set; }
    }
}
```

- [ ] **Step 5: Wire Admin Mode into `ResumePage.razor`**

Replace `src/Resume.BlazorApp.Client/Pages/ResumePage.razor` with:
```razor
@page "/"
@rendermode InteractiveAuto
@using Resume.Contracts
@inject ResumeApiClient Api

<PageTitle>Resume</PageTitle>

<div class="resume-page">
    <div class="resume-page__toolbar">
        <button @onclick="ToggleAdminMode">@(_adminMode ? "Exit Admin Mode" : "Admin Mode")</button>
    </div>

    @if (_personalInfo is null)
    {
        <p>Loading...</p>
    }
    else
    {
        <ResumeSection Title="Personal Info" InitiallyExpanded="true">
            <p><strong>@_personalInfo.FullName</strong> — @_personalInfo.Headline</p>
            <p>@_personalInfo.Summary</p>
            <p>
                @_personalInfo.Email
                @if (_personalInfo.Phone is not null)
                {
                    <text> • @_personalInfo.Phone</text>
                }
            </p>
            @if (_adminMode)
            {
                <PersonalInfoForm PersonalInfo="_personalInfo" OnSaved="HandleSaved" />
            }
        </ResumeSection>

        <ResumeSection Title="Skills">
            @foreach (var group in _skills.GroupBy(s => s.Category))
            {
                <h4>@group.Key</h4>
                <ul>
                    @foreach (var skill in group.OrderBy(s => s.SortOrder))
                    {
                        <li>
                            @skill.Name
                            @if (_adminMode)
                            {
                                <button @onclick="() => _editingSkill = skill">Edit</button>
                            }
                        </li>
                    }
                </ul>
            }
            @if (_adminMode)
            {
                <SkillForm EditingSkill="_editingSkill" OnSaved="HandleSkillSaved" />
            }
        </ResumeSection>

        <ResumeSection Title="Experience">
            @foreach (var job in _experience)
            {
                <div class="resume-entry">
                    <h4>
                        @job.JobTitle, @job.Company
                        @if (_adminMode)
                        {
                            <button @onclick="() => _editingExperience = job">Edit</button>
                        }
                    </h4>
                    <p>@job.StartDate.ToString("MMM yyyy") - @(job.EndDate?.ToString("MMM yyyy") ?? "Present")</p>
                    <ul>
                        @foreach (var highlight in job.Highlights)
                        {
                            <li>@highlight</li>
                        }
                    </ul>
                </div>
            }
            @if (_adminMode)
            {
                <ExperienceForm EditingEntry="_editingExperience" OnSaved="HandleExperienceSaved" />
            }
        </ResumeSection>

        <ResumeSection Title="Education">
            @foreach (var edu in _education)
            {
                <div class="resume-entry">
                    <h4>
                        @edu.Degree, @edu.Institution
                        @if (_adminMode)
                        {
                            <button @onclick="() => _editingEducation = edu">Edit</button>
                        }
                    </h4>
                    <p>@edu.StartDate.ToString("MMM yyyy") - @(edu.EndDate?.ToString("MMM yyyy") ?? "Present")</p>
                </div>
            }
            @if (_adminMode)
            {
                <EducationForm EditingEntry="_editingEducation" OnSaved="HandleEducationSaved" />
            }
        </ResumeSection>
    }

    @if (_banner is not null)
    {
        <div class="resume-banner">@_banner</div>
    }
</div>

@code {
    private const string SavedBanner = "Saved — but this is a demo; changes are not actually persisted.";

    private PersonalInfoResponse? _personalInfo;
    private List<SkillResponse> _skills = [];
    private List<ExperienceResponse> _experience = [];
    private List<EducationResponse> _education = [];
    private bool _adminMode;
    private string? _banner;
    private SkillResponse? _editingSkill;
    private ExperienceResponse? _editingExperience;
    private EducationResponse? _editingEducation;

    protected override async Task OnInitializedAsync() => await LoadAllAsync();

    private async Task LoadAllAsync()
    {
        _personalInfo = await Api.GetPersonalInfoAsync();
        _skills = await Api.GetSkillsAsync();
        _experience = await Api.GetExperienceAsync();
        _education = await Api.GetEducationAsync();
    }

    private void ToggleAdminMode() => _adminMode = !_adminMode;

    private async Task HandleSaved()
    {
        _banner = SavedBanner;
        await LoadAllAsync();
    }

    private async Task HandleSkillSaved()
    {
        _editingSkill = null;
        _banner = SavedBanner;
        await LoadAllAsync();
    }

    private async Task HandleExperienceSaved()
    {
        _editingExperience = null;
        _banner = SavedBanner;
        await LoadAllAsync();
    }

    private async Task HandleEducationSaved()
    {
        _editingEducation = null;
        _banner = SavedBanner;
        await LoadAllAsync();
    }
}
```

Add the toolbar CSS to `src/Resume.BlazorApp/wwwroot/app.css`:
```css
.resume-page__toolbar {
    display: flex;
    justify-content: flex-end;
    margin-bottom: 1rem;
}
```

- [ ] **Step 6: Build and manually verify**

Run: `dotnet build Resume.sln`
Expected: Build succeeds with 0 errors.

Run: `dotnet run --project src/Resume.AppHost`, open the `blazorapp` URL.
Expected: Clicking "Admin Mode" reveals edit controls; submitting the Personal Info form shows the banner and the page reverts to "Jordan Rivera" after reload; adding a Skill shows the banner and the skill list stays at 10 items after reload. Stop the AppHost once confirmed.

- [ ] **Step 7: Commit**

```bash
git add src/Resume.BlazorApp src/Resume.BlazorApp.Client
git commit -m "Add Blazor Admin Mode with create/edit forms"
```

---

### Task 12: Resume.React scaffold, AppHost wiring, and smoke-tested API client

**Files:**
- Create: `src/Resume.React/*` (via Vite scaffold)
- Create: `src/Resume.React/src/types.ts`, `src/Resume.React/src/api/client.ts`
- Modify: `src/Resume.React/vite.config.ts`, `src/Resume.React/src/App.tsx`
- Modify: `src/Resume.AppHost/Program.cs`, `src/Resume.AppHost/Resume.AppHost.csproj`
- Modify: `src/Resume.Api/Program.cs` (CORS already added in Task 4 — no change needed here, verified in Step 6)

**Interfaces:**
- Produces: `src/Resume.React/src/api/client.ts`'s `api` object with `getPersonalInfo`, `updatePersonalInfo`, `getSkills`, `createSkill`, `updateSkill`, `getExperience`, `createExperience`, `updateExperience`, `getEducation`, `createEducation`, `updateEducation` — consumed by `App.tsx` in Tasks 13-14.

- [ ] **Step 1: Scaffold the Vite + React + TypeScript app**

```bash
npm create vite@latest Resume.React -- --template react-ts
```
(run from `src/`, so the app lands at `src/Resume.React/`)

```bash
cd src/Resume.React
npm install
cd ../..
```

- [ ] **Step 2: Configure the dev server port from the `PORT` env var (Aspire assigns it)**

Replace `src/Resume.React/vite.config.ts`:
```typescript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: Number(process.env.PORT) || 5173,
  },
})
```

- [ ] **Step 3: Add the shared TypeScript types matching `Resume.Contracts`**

`src/Resume.React/src/types.ts`:
```typescript
export interface PersonalInfo {
  id: string;
  fullName: string;
  headline: string;
  email: string;
  phone: string | null;
  summary: string;
  linkedInUrl: string | null;
  gitHubUrl: string | null;
  websiteUrl: string | null;
}

export interface Skill {
  id: string;
  category: string;
  name: string;
  sortOrder: number;
}

export interface Experience {
  id: string;
  company: string;
  jobTitle: string;
  location: string | null;
  startDate: string;
  endDate: string | null;
  highlights: string[];
}

export interface Education {
  id: string;
  institution: string;
  degree: string;
  fieldOfStudy: string | null;
  startDate: string;
  endDate: string | null;
  details: string[];
}
```

- [ ] **Step 4: Add the API client**

`src/Resume.React/src/api/client.ts`:
```typescript
import type { PersonalInfo, Skill, Experience, Education } from '../types'

const API_BASE_URL = import.meta.env.VITE_API_URL as string

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  })

  if (!response.ok) {
    throw new Error(`Request to ${path} failed with status ${response.status}`)
  }

  return (await response.json()) as T
}

export const api = {
  getPersonalInfo: () => request<PersonalInfo>('/api/personal-info'),
  updatePersonalInfo: (body: Omit<PersonalInfo, 'id'>) =>
    request<PersonalInfo>('/api/personal-info', { method: 'PUT', body: JSON.stringify(body) }),

  getSkills: () => request<Skill[]>('/api/skills'),
  createSkill: (body: Omit<Skill, 'id'>) =>
    request<Skill>('/api/skills', { method: 'POST', body: JSON.stringify(body) }),
  updateSkill: (skill: Skill) =>
    request<Skill>(`/api/skills/${skill.id}`, { method: 'PUT', body: JSON.stringify(skill) }),

  getExperience: () => request<Experience[]>('/api/experience'),
  createExperience: (body: Omit<Experience, 'id'>) =>
    request<Experience>('/api/experience', { method: 'POST', body: JSON.stringify(body) }),
  updateExperience: (entry: Experience) =>
    request<Experience>(`/api/experience/${entry.id}`, { method: 'PUT', body: JSON.stringify(entry) }),

  getEducation: () => request<Education[]>('/api/education'),
  createEducation: (body: Omit<Education, 'id'>) =>
    request<Education>('/api/education', { method: 'POST', body: JSON.stringify(body) }),
  updateEducation: (entry: Education) =>
    request<Education>(`/api/education/${entry.id}`, { method: 'PUT', body: JSON.stringify(entry) }),
}
```

- [ ] **Step 5: Replace `App.tsx` with a smoke-test render**

Replace `src/Resume.React/src/App.tsx`:
```tsx
import { useEffect, useState } from 'react'
import { api } from './api/client'
import type { PersonalInfo } from './types'
import './App.css'

function App() {
  const [personalInfo, setPersonalInfo] = useState<PersonalInfo | null>(null)

  useEffect(() => {
    api.getPersonalInfo().then(setPersonalInfo)
  }, [])

  if (!personalInfo) {
    return <p>Loading...</p>
  }

  return (
    <div>
      <h1>{personalInfo.fullName}</h1>
      <p>{personalInfo.headline}</p>
    </div>
  )
}

export default App
```

- [ ] **Step 6: Wire `Resume.React` into the AppHost**

```bash
dotnet add src/Resume.AppHost/Resume.AppHost.csproj package Aspire.Hosting.NodeJs --version 9.5.2
```

Modify `src/Resume.AppHost/Program.cs`, adding after the `blazorApp` resource:
```csharp
var react = builder.AddNpmApp("react", "../Resume.React", "dev")
    .WithHttpEndpoint(env: "PORT", port: 5173)
    .WithEnvironment("VITE_API_URL", api.GetEndpoint("https"))
    .WaitFor(api)
    .WithExternalHttpEndpoints();
```

- [ ] **Step 7: Build and manually verify**

Run: `dotnet build Resume.sln`
Expected: Build succeeds with 0 errors.

Run: `dotnet run --project src/Resume.AppHost` (Docker Desktop running), open the `react` resource's URL from the Aspire dashboard.
Expected: The page loads and displays "Jordan Rivera" / "Senior Software Engineer" — confirming CORS (added to `Resume.Api` in Task 4) allows the cross-origin request. Stop the AppHost once confirmed.

- [ ] **Step 8: Commit**

```bash
git add src/Resume.React src/Resume.AppHost Resume.sln
git commit -m "Add Resume.React scaffold with Aspire wiring and smoke-tested API client"
```

(Note: `Resume.React` is not part of `Resume.sln`/MSBuild — it's a Node project referenced only from `Resume.AppHost/Program.cs`, which is the standard Aspire pattern for `AddNpmApp`.)

---

### Task 13: React full resume UI (read-only)

**Files:**
- Create: `src/Resume.React/src/components/ResumeSection.tsx`
- Modify: `src/Resume.React/src/App.tsx`
- Modify: `src/Resume.React/src/App.css`

**Interfaces:**
- Consumes: `api` client (Task 12).
- Produces: `ResumeSection` component with props `{ title: string, initiallyExpanded?: boolean, children: ReactNode }` — consumed again by Task 14's admin forms.

- [ ] **Step 1: Add the collapsible, sticky-header `ResumeSection` component**

`src/Resume.React/src/components/ResumeSection.tsx`:
```tsx
import { useState, type ReactNode } from 'react'

interface ResumeSectionProps {
  title: string
  initiallyExpanded?: boolean
  children: ReactNode
}

export function ResumeSection({ title, initiallyExpanded = false, children }: ResumeSectionProps) {
  const [expanded, setExpanded] = useState(initiallyExpanded)

  return (
    <div className="resume-section">
      <button
        className="resume-section__header"
        onClick={() => setExpanded((prev) => !prev)}
        aria-expanded={expanded}
      >
        <span>{title}</span>
        <span className="resume-section__chevron">{expanded ? '▾' : '▸'}</span>
      </button>
      {expanded && <div className="resume-section__content">{children}</div>}
    </div>
  )
}
```

- [ ] **Step 2: Add the sticky-header and section styling**

Replace `src/Resume.React/src/App.css`:
```css
.resume-page {
  max-width: 800px;
  margin: 0 auto;
  padding: 1rem;
  text-align: left;
}

.resume-section {
  border: 1px solid #d0d7de;
  border-radius: 8px;
  margin-bottom: 1rem;
  overflow: hidden;
}

.resume-section__header {
  position: sticky;
  top: 0;
  z-index: 1;
  width: 100%;
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.75rem 1rem;
  background: #f6f8fa;
  border: none;
  border-bottom: 1px solid #d0d7de;
  font-size: 1.1rem;
  font-weight: 600;
  cursor: pointer;
  text-align: left;
}

.resume-section__content {
  padding: 1rem;
}

.resume-entry {
  margin-bottom: 1rem;
}

.resume-banner {
  position: sticky;
  bottom: 0;
  background: #fff8c5;
  border: 1px solid #d4a72c;
  border-radius: 6px;
  padding: 0.75rem 1rem;
  margin-top: 1rem;
}
```

- [ ] **Step 3: Replace `App.tsx` with the full composed resume page**

```tsx
import { useEffect, useState } from 'react'
import { api } from './api/client'
import type { PersonalInfo, Skill, Experience, Education } from './types'
import { ResumeSection } from './components/ResumeSection'
import './App.css'

function App() {
  const [personalInfo, setPersonalInfo] = useState<PersonalInfo | null>(null)
  const [skills, setSkills] = useState<Skill[]>([])
  const [experience, setExperience] = useState<Experience[]>([])
  const [education, setEducation] = useState<Education[]>([])

  const loadAll = async () => {
    setPersonalInfo(await api.getPersonalInfo())
    setSkills(await api.getSkills())
    setExperience(await api.getExperience())
    setEducation(await api.getEducation())
  }

  useEffect(() => {
    loadAll()
  }, [])

  if (!personalInfo) {
    return <p>Loading...</p>
  }

  const skillsByCategory = skills.reduce<Record<string, Skill[]>>((groups, skill) => {
    ;(groups[skill.category] ??= []).push(skill)
    return groups
  }, {})

  return (
    <div className="resume-page">
      <ResumeSection title="Personal Info" initiallyExpanded>
        <p>
          <strong>{personalInfo.fullName}</strong> — {personalInfo.headline}
        </p>
        <p>{personalInfo.summary}</p>
        <p>
          {personalInfo.email}
          {personalInfo.phone && ` • ${personalInfo.phone}`}
        </p>
      </ResumeSection>

      <ResumeSection title="Skills">
        {Object.entries(skillsByCategory).map(([category, items]) => (
          <div key={category}>
            <h4>{category}</h4>
            <ul>
              {items
                .slice()
                .sort((a, b) => a.sortOrder - b.sortOrder)
                .map((skill) => (
                  <li key={skill.id}>{skill.name}</li>
                ))}
            </ul>
          </div>
        ))}
      </ResumeSection>

      <ResumeSection title="Experience">
        {experience.map((job) => (
          <div className="resume-entry" key={job.id}>
            <h4>
              {job.jobTitle}, {job.company}
            </h4>
            <p>
              {job.startDate} - {job.endDate ?? 'Present'}
            </p>
            <ul>
              {job.highlights.map((highlight, index) => (
                <li key={index}>{highlight}</li>
              ))}
            </ul>
          </div>
        ))}
      </ResumeSection>

      <ResumeSection title="Education">
        {education.map((edu) => (
          <div className="resume-entry" key={edu.id}>
            <h4>
              {edu.degree}, {edu.institution}
            </h4>
            <p>
              {edu.startDate} - {edu.endDate ?? 'Present'}
            </p>
          </div>
        ))}
      </ResumeSection>
    </div>
  )
}

export default App
```

- [ ] **Step 4: Build and manually verify**

Run: `dotnet run --project src/Resume.AppHost`, open the `react` resource's URL.
Expected: Personal Info is expanded by default; Skills/Experience/Education are collapsed and expand on click; a section header stays pinned to the top while scrolling its expanded content. Stop the AppHost once confirmed.

- [ ] **Step 5: Commit**

```bash
git add src/Resume.React
git commit -m "Add full read-only React resume UI with collapsible sticky-header sections"
```

---

### Task 14: React Admin Mode (create/edit forms)

**Files:**
- Create: `src/Resume.React/src/components/PersonalInfoForm.tsx`, `SkillForm.tsx`, `ExperienceForm.tsx`, `EducationForm.tsx`
- Modify: `src/Resume.React/src/App.tsx`, `src/Resume.React/src/App.css`

**Interfaces:**
- Consumes: `api` client (Task 12), `ResumeSection` (Task 13).

- [ ] **Step 1: Add the PersonalInfo edit form**

`src/Resume.React/src/components/PersonalInfoForm.tsx`:
```tsx
import { useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type { PersonalInfo } from '../types'

interface Props {
  personalInfo: PersonalInfo
  onSaved: () => void
}

export function PersonalInfoForm({ personalInfo, onSaved }: Props) {
  const [fullName, setFullName] = useState(personalInfo.fullName)
  const [headline, setHeadline] = useState(personalInfo.headline)
  const [email, setEmail] = useState(personalInfo.email)
  const [summary, setSummary] = useState(personalInfo.summary)

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    await api.updatePersonalInfo({ ...personalInfo, fullName, headline, email, summary })
    onSaved()
  }

  return (
    <form onSubmit={handleSubmit}>
      <div>
        <label>
          Full name
          <input value={fullName} onChange={(e) => setFullName(e.target.value)} />
        </label>
      </div>
      <div>
        <label>
          Headline
          <input value={headline} onChange={(e) => setHeadline(e.target.value)} />
        </label>
      </div>
      <div>
        <label>
          Email
          <input value={email} onChange={(e) => setEmail(e.target.value)} />
        </label>
      </div>
      <div>
        <label>
          Summary
          <textarea value={summary} onChange={(e) => setSummary(e.target.value)} />
        </label>
      </div>
      <button type="submit">Save</button>
    </form>
  )
}
```

- [ ] **Step 2: Add the Skill add/edit form**

`src/Resume.React/src/components/SkillForm.tsx`:
```tsx
import { useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type { Skill } from '../types'

interface Props {
  editingSkill: Skill | null
  onSaved: () => void
}

export function SkillForm({ editingSkill, onSaved }: Props) {
  const [category, setCategory] = useState(editingSkill?.category ?? '')
  const [name, setName] = useState(editingSkill?.name ?? '')
  const [sortOrder, setSortOrder] = useState(editingSkill?.sortOrder ?? 0)

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (editingSkill) {
      await api.updateSkill({ id: editingSkill.id, category, name, sortOrder })
    } else {
      await api.createSkill({ category, name, sortOrder })
    }
    onSaved()
  }

  return (
    <form onSubmit={handleSubmit}>
      <div>
        <label>
          Category
          <input value={category} onChange={(e) => setCategory(e.target.value)} />
        </label>
      </div>
      <div>
        <label>
          Name
          <input value={name} onChange={(e) => setName(e.target.value)} />
        </label>
      </div>
      <div>
        <label>
          Sort order
          <input type="number" value={sortOrder} onChange={(e) => setSortOrder(Number(e.target.value))} />
        </label>
      </div>
      <button type="submit">{editingSkill ? 'Save' : 'Add'}</button>
    </form>
  )
}
```

- [ ] **Step 3: Add the Experience add/edit form**

`src/Resume.React/src/components/ExperienceForm.tsx`:
```tsx
import { useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type { Experience } from '../types'

interface Props {
  editingEntry: Experience | null
  onSaved: () => void
}

export function ExperienceForm({ editingEntry, onSaved }: Props) {
  const [company, setCompany] = useState(editingEntry?.company ?? '')
  const [jobTitle, setJobTitle] = useState(editingEntry?.jobTitle ?? '')
  const [startDate, setStartDate] = useState(editingEntry?.startDate ?? new Date().toISOString().slice(0, 10))
  const [highlightsText, setHighlightsText] = useState(editingEntry?.highlights.join('\n') ?? '')

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    const highlights = highlightsText.split('\n').map((h) => h.trim()).filter(Boolean)

    if (editingEntry) {
      await api.updateExperience({ ...editingEntry, company, jobTitle, startDate, highlights })
    } else {
      await api.createExperience({ company, jobTitle, location: null, startDate, endDate: null, highlights })
    }
    onSaved()
  }

  return (
    <form onSubmit={handleSubmit}>
      <div>
        <label>
          Company
          <input value={company} onChange={(e) => setCompany(e.target.value)} />
        </label>
      </div>
      <div>
        <label>
          Job title
          <input value={jobTitle} onChange={(e) => setJobTitle(e.target.value)} />
        </label>
      </div>
      <div>
        <label>
          Start date
          <input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
        </label>
      </div>
      <div>
        <label>
          Highlights (one per line)
          <textarea value={highlightsText} onChange={(e) => setHighlightsText(e.target.value)} />
        </label>
      </div>
      <button type="submit">{editingEntry ? 'Save' : 'Add'}</button>
    </form>
  )
}
```

- [ ] **Step 4: Add the Education add/edit form**

`src/Resume.React/src/components/EducationForm.tsx`:
```tsx
import { useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type { Education } from '../types'

interface Props {
  editingEntry: Education | null
  onSaved: () => void
}

export function EducationForm({ editingEntry, onSaved }: Props) {
  const [institution, setInstitution] = useState(editingEntry?.institution ?? '')
  const [degree, setDegree] = useState(editingEntry?.degree ?? '')
  const [startDate, setStartDate] = useState(editingEntry?.startDate ?? new Date().toISOString().slice(0, 10))

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()

    if (editingEntry) {
      await api.updateEducation({ ...editingEntry, institution, degree, startDate })
    } else {
      await api.createEducation({ institution, degree, fieldOfStudy: null, startDate, endDate: null, details: [] })
    }
    onSaved()
  }

  return (
    <form onSubmit={handleSubmit}>
      <div>
        <label>
          Institution
          <input value={institution} onChange={(e) => setInstitution(e.target.value)} />
        </label>
      </div>
      <div>
        <label>
          Degree
          <input value={degree} onChange={(e) => setDegree(e.target.value)} />
        </label>
      </div>
      <div>
        <label>
          Start date
          <input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
        </label>
      </div>
      <button type="submit">{editingEntry ? 'Save' : 'Add'}</button>
    </form>
  )
}
```

- [ ] **Step 5: Wire Admin Mode into `App.tsx`**

Replace `src/Resume.React/src/App.tsx`:
```tsx
import { useEffect, useState } from 'react'
import { api } from './api/client'
import type { PersonalInfo, Skill, Experience, Education } from './types'
import { ResumeSection } from './components/ResumeSection'
import { PersonalInfoForm } from './components/PersonalInfoForm'
import { SkillForm } from './components/SkillForm'
import { ExperienceForm } from './components/ExperienceForm'
import { EducationForm } from './components/EducationForm'
import './App.css'

const SAVED_BANNER = 'Saved — but this is a demo; changes are not actually persisted.'

function App() {
  const [personalInfo, setPersonalInfo] = useState<PersonalInfo | null>(null)
  const [skills, setSkills] = useState<Skill[]>([])
  const [experience, setExperience] = useState<Experience[]>([])
  const [education, setEducation] = useState<Education[]>([])
  const [adminMode, setAdminMode] = useState(false)
  const [banner, setBanner] = useState<string | null>(null)
  const [editingSkill, setEditingSkill] = useState<Skill | null>(null)
  const [editingExperience, setEditingExperience] = useState<Experience | null>(null)
  const [editingEducation, setEditingEducation] = useState<Education | null>(null)

  const loadAll = async () => {
    setPersonalInfo(await api.getPersonalInfo())
    setSkills(await api.getSkills())
    setExperience(await api.getExperience())
    setEducation(await api.getEducation())
  }

  useEffect(() => {
    loadAll()
  }, [])

  const handleSaved = async () => {
    setEditingSkill(null)
    setEditingExperience(null)
    setEditingEducation(null)
    setBanner(SAVED_BANNER)
    await loadAll()
  }

  if (!personalInfo) {
    return <p>Loading...</p>
  }

  const skillsByCategory = skills.reduce<Record<string, Skill[]>>((groups, skill) => {
    ;(groups[skill.category] ??= []).push(skill)
    return groups
  }, {})

  return (
    <div className="resume-page">
      <div className="resume-page__toolbar">
        <button onClick={() => setAdminMode((prev) => !prev)}>
          {adminMode ? 'Exit Admin Mode' : 'Admin Mode'}
        </button>
      </div>

      <ResumeSection title="Personal Info" initiallyExpanded>
        <p>
          <strong>{personalInfo.fullName}</strong> — {personalInfo.headline}
        </p>
        <p>{personalInfo.summary}</p>
        <p>
          {personalInfo.email}
          {personalInfo.phone && ` • ${personalInfo.phone}`}
        </p>
        {adminMode && <PersonalInfoForm personalInfo={personalInfo} onSaved={handleSaved} />}
      </ResumeSection>

      <ResumeSection title="Skills">
        {Object.entries(skillsByCategory).map(([category, items]) => (
          <div key={category}>
            <h4>{category}</h4>
            <ul>
              {items
                .slice()
                .sort((a, b) => a.sortOrder - b.sortOrder)
                .map((skill) => (
                  <li key={skill.id}>
                    {skill.name}
                    {adminMode && <button onClick={() => setEditingSkill(skill)}>Edit</button>}
                  </li>
                ))}
            </ul>
          </div>
        ))}
        {adminMode && <SkillForm editingSkill={editingSkill} onSaved={handleSaved} />}
      </ResumeSection>

      <ResumeSection title="Experience">
        {experience.map((job) => (
          <div className="resume-entry" key={job.id}>
            <h4>
              {job.jobTitle}, {job.company}
              {adminMode && <button onClick={() => setEditingExperience(job)}>Edit</button>}
            </h4>
            <p>
              {job.startDate} - {job.endDate ?? 'Present'}
            </p>
            <ul>
              {job.highlights.map((highlight, index) => (
                <li key={index}>{highlight}</li>
              ))}
            </ul>
          </div>
        ))}
        {adminMode && <ExperienceForm editingEntry={editingExperience} onSaved={handleSaved} />}
      </ResumeSection>

      <ResumeSection title="Education">
        {education.map((edu) => (
          <div className="resume-entry" key={edu.id}>
            <h4>
              {edu.degree}, {edu.institution}
              {adminMode && <button onClick={() => setEditingEducation(edu)}>Edit</button>}
            </h4>
            <p>
              {edu.startDate} - {edu.endDate ?? 'Present'}
            </p>
          </div>
        ))}
        {adminMode && <EducationForm editingEntry={editingEducation} onSaved={handleSaved} />}
      </ResumeSection>

      {banner && <div className="resume-banner">{banner}</div>}
    </div>
  )
}

export default App
```

Append to `src/Resume.React/src/App.css`:
```css
.resume-page__toolbar {
  display: flex;
  justify-content: flex-end;
  margin-bottom: 1rem;
}
```

- [ ] **Step 6: Build and manually verify**

Run: `dotnet run --project src/Resume.AppHost`, open the `react` resource's URL.
Expected: Clicking "Admin Mode" reveals edit controls; submitting the Personal Info form shows the banner and reverts to "Jordan Rivera" after reload; adding a Skill shows the banner and the skill list stays at 10 items after reload. Stop the AppHost once confirmed.

- [ ] **Step 7: Commit**

```bash
git add src/Resume.React
git commit -m "Add React Admin Mode with create/edit forms"
```

---

### Task 15: Final end-to-end verification and README

**Files:**
- Create: `README.md`

**Interfaces:** None (verification-only task; no new production interfaces).

- [ ] **Step 1: Run the full test suite**

Run: `dotnet test Resume.sln`
Expected: All tests pass (19 in `Resume.Api.Tests`: 4 `DataSeedTests` + 3 `PersonalInfoEndpointTests` + 4 each for `SkillsEndpointTests`, `ExperienceEndpointTests`, `EducationEndpointTests`).

- [ ] **Step 2: Run the full AppHost and verify every resource comes up healthy**

Run: `dotnet run --project src/Resume.AppHost` (Docker Desktop running).
Expected: The Aspire dashboard shows `postgres`, `api`, `blazorapp`, and `react` all reaching a running/healthy state with no restart loops.

- [ ] **Step 3: Verify both frontends render identical resume data**

Open both the `blazorapp` and `react` resource URLs from the dashboard.
Expected: Both show "Jordan Rivera" / "Senior Software Engineer", the same 4 skill categories, the same 2 experience entries, and the same 1 education entry. Expand/collapse each section in both apps and confirm sticky headers behave the same way.

- [ ] **Step 4: Verify the fake-write behavior end-to-end in both frontends**

In each frontend: enable Admin Mode, submit an edit to Personal Info, confirm the banner appears, then reload the page and confirm the original seeded data is shown (not the edit).
Expected: Consistent in both `blazorapp` and `react`. Stop the AppHost once confirmed.

- [ ] **Step 5: Add the README**

`README.md`:
```markdown
# Resume Web Application

A .NET 10 resume site with dual frontends (Blazor Web App and React), a
FastEndpoints vertical-slice API, and PostgreSQL, orchestrated locally with
.NET Aspire. Write (create/edit) endpoints validate input but never persist
changes — the database always reflects the original seeded resume data.

## Prerequisites

- .NET 10 SDK
- Node.js 22+
- Docker Desktop (for the Postgres container)

## Running locally

```bash
dotnet run --project src/Resume.AppHost
```

Open the Aspire dashboard URL printed in the console to find the running
resources: `postgres`, `api`, `blazorapp`, and `react`.

## Running tests

```bash
dotnet test Resume.sln
```

## Project layout

See `docs/superpowers/specs/2026-08-22-resume-app-design.md` for the full
design spec.
```

- [ ] **Step 6: Commit**

```bash
git add README.md
git commit -m "Add README with run and test instructions"
```

---

## Spec Coverage Check

- Personal info (no address), skills, experience, education → Tasks 2, 4-7 (data + API), 10, 13 (UI).
- Create/edit endpoints that never persist → Tasks 4-7 (`SaveChangesAsync` never called, verified by tests).
- Postgres in a container → Task 1 (`AddPostgres`), Task 3 (migrations), Task 8 (real-data verification).
- Blazor + React dual frontends → Tasks 9-11 (Blazor), 12-14 (React).
- .NET Aspire orchestration → Tasks 1, 8, 9, 12, 15.
- Vertical slice / FastEndpoints API → Tasks 4-7.
- Expandable sections with sticky headers → Tasks 10, 13 (`ResumeSection` / `.resume-section__header { position: sticky }`).
- Admin functionality calling the (no-op) API → Tasks 11, 14.
- Unit tests for API slices → Tasks 2, 4-7.
