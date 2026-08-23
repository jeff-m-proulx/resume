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

Start everything via the AppHost:

```bash
dotnet run --project src/Resume.AppHost
```

The AppHost installs the React app's npm packages automatically on first
run (via `WithNpmPackageInstallation()`) — no separate `npm install` step
needed.

Open the Aspire dashboard URL printed in the console to find the running
resources: `postgres`, `pgadmin`, `api`, `blazorapp`, and `react`. `pgadmin`
is a [pgAdmin](https://www.pgadmin.org/) UI pre-configured with a connection
to the local `postgres` server — open its URL from the dashboard to browse
`resumedb` directly, no manual connection setup required.

## Running tests

```bash
dotnet test Resume.sln
```

## Project layout

See `docs/superpowers/specs/2026-08-22-resume-app-design.md` for the full
design spec.
