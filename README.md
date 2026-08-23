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
