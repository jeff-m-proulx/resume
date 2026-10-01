# Resume Web Application

A .NET 10 resume site with dual frontends (Blazor Web App and React), a
FastEndpoints vertical-slice API, and PostgreSQL, orchestrated locally with
.NET Aspire. Write (create/edit) endpoints validate input but never persist
changes — the database always reflects the original seeded resume data.

## Prerequisites

- .NET 10 SDK
- Node.js 22+
- Docker Desktop (for the Postgres container, and to build the React image
  when deploying)

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

## Deploying to Azure

Deployment goes through the [Azure Developer CLI](https://aka.ms/azd) (`azd`),
which reads `azure.yaml` and publishes the Aspire app to Azure Container Apps.

Prerequisites: `azd` (`winget install microsoft.azd`), an Azure subscription,
and a Postgres connection string. In publish mode the AppHost does not
provision a database — `resumedb` becomes an external connection string, so
point it at a managed Postgres such as [Neon](https://neon.tech).

```bash
azd auth login
azd up
```

`azd up` prompts for a subscription, a region, and the `resumedb` connection
string (stored as a secret in the azd environment, never in the repo). It then
provisions the container environment and deploys three container apps:

| App | Ingress | Notes |
| --- | --- | --- |
| `api` | public | Applies EF migrations on startup, which carry the seed data |
| `blazorapp` | public | Server-rendered site; proxies `/api` to `api` via YARP |
| `react` | public | Static bundle behind nginx, which proxies `/api/` to `api` |

Re-deploy code without re-provisioning with `azd deploy`; tear the whole
environment down with `azd down`.

### How the React app is built for Azure

Locally the `react` resource runs the Vite dev server. In publish mode the
AppHost switches it to `PublishAsDockerFile()`, building
`src/Resume.React/Dockerfile` — a Vite production build served by nginx.

Vite inlines `import.meta.env.*` at build time, but the API's URL is not known
until after provisioning. Rather than bake it in, the deployed bundle calls
`/api/...` on its own origin and nginx proxies those requests onward, using the
`API_URL` environment variable the AppHost supplies at container start.

The About page's link to the other UI works the same way. Each site links to
`/switch-ui` on its own origin and redirects from there — a minimal API
endpoint on the Blazor host, an nginx `return 302` in the React container —
so neither frontend needs the other's address at build time. The AppHost
supplies both addresses: `ReactAppUrl` to `blazorapp`, and `BLAZOR_URL` (or
`VITE_BLAZOR_URL` in local development) to `react`.

### Costs

Container Apps' monthly free grant covers a low-traffic site's active usage,
but not replicas kept warm around the clock: Azure bills an idle replica too,
and three of them cost ~$35/month. azd's generated Container App templates pin
`minReplicas: 1`, so the AppHost carries its own copies in
`src/Resume.AppHost/infra/*.tmpl.yaml` with `minReplicas: 0`. azd picks those
up in place of its defaults; the trade-off is a cold start of a few seconds on
the first request after a quiet spell.

azd also adds an Aspire dashboard to the Container Apps environment
(~$3.50/month). The dashboard is only wanted for local runs, so the
environment's Bicep is committed in `infra/` with the dashboard removed, and
azd provisions from it instead of generating its own. The catch is that a new
Azure resource in the AppHost no longer reaches `infra/` on its own: rerun
`azd infra generate`, then delete the dashboard from `infra/resources.bicep`
again and restore `minReplicas: 0` in the `.tmpl.yaml` files, both of which
it overwrites. (A new project or container needs no regeneration, but does
start with azd's default template, so give it a `.tmpl.yaml` of its own.)

What remains is the container registry (~$5/month). Neon's free tier covers
the database.

## Running tests

```bash
dotnet test Resume.sln
```

## Project layout

See `docs/superpowers/specs/2026-08-22-resume-app-design.md` for the full
design spec, and
`docs/superpowers/specs/2026-09-05-about-page-and-footer-design.md` for the
About page and site footer.
