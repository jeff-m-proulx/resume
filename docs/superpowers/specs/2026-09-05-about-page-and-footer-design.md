# About Page and Site Footer — Design Spec

Date: 2026-09-05

## Summary

Add an About page to both frontends describing the technologies the
application is built on, including the Azure deployment work that has not
been implemented yet. Add a site footer to both frontends, styled in the same
blue as the resume header, holding a centered links row (initially just
"About") above a smaller copyright line. The About page also carries a link
that switches the visitor to the other UI — React from Blazor, Blazor from
React.

## Goals

- A single About page, implemented independently in each frontend, that lists
  the technology stack, separating what is built and deployed from the
  deployment work still outstanding.
- The About page states which UI the visitor is currently viewing and links
  to the equivalent page in the other UI.
- A footer on every page of both frontends, matching the header's blue
  (`#1f3b57`), with centered content: a links row on top, a smaller copyright
  line beneath, separated by enough space that additional links can be added
  later without redesigning the footer.
- The switch link resolves the other UI's address at runtime, in both local
  development and a deployed environment. Nothing about either URL is
  compiled into the React bundle.

## Non-goals

- A shared component library between Blazor and React. Each implements the
  About page and footer independently, following the existing convention in
  `2026-08-22-resume-app-design.md`.
- Implementing any of the outstanding Azure work the About page lists. This
  spec only describes that work; it adds no pipelines, telemetry exporters, or
  identity configuration.
- A general-purpose routing library for the React app. One extra page does not
  justify `react-router-dom`.
- Automated frontend tests. Frontend test infrastructure remains a non-goal of
  the project, per the original spec.
- Localization of the About copy, or sourcing its content from the database.
  The content is static markup in each frontend.

## Footer

### Layout

```
+----------------------------------------------+
|                                              |
|                    About                     |  links row
|                                              |
|                                              |  ~1.75rem separation
|           (c) 2026 Jeffrey M. Proulx         |  copyright, smaller
|                                              |
+----------------------------------------------+
```

- The footer sits at the end of the page flow. It is not sticky: the resume
  header is already sticky and consumes vertical space, and a second pinned
  band would leave too little room for content on short viewports.
- The links row is a `<nav>` element containing the links as a centered flex
  row with a `1.5rem` gap, so a second or third link is a one-line addition
  with the spacing already correct. It contains only "About" for now.
- The copyright line reads `© 2026 Jeffrey M. Proulx` at `0.8rem`, one step
  below the link text.
- Everything is centered horizontally.

### Styling

The footer reuses the header's palette so the two bookend the page:

| Property | Value |
|---|---|
| background | `#1f3b57` (same as `.resume-header`) |
| border-radius | `8px` |
| padding | `1.5rem 1rem` |
| margin-top | `1.5rem` |
| link color | `#ffffff`, underlined on hover/focus |
| copyright color | `#d9e3ec` (same as `.resume-header__headline`) |
| focus ring | `2px solid #ffffff`, `outline-offset: 2px` |

Class names follow the existing BEM-ish convention: `.resume-footer`,
`.resume-footer__links`, `.resume-footer__link`, `.resume-footer__copyright`.

The rules are duplicated in `src/Resume.BlazorApp/wwwroot/app.css` and
`src/Resume.React/src/App.css`, matching how `.resume-header`,
`.resume-section`, and `.resume-subsection` are already duplicated across the
two frontends.

### Placement

- **Blazor** — rendered in `MainLayout.razor` after `@Body`, so it appears on
  the resume page, the About page, and the not-found page alike.
- **React** — a `ResumeFooter` component rendered from `App.tsx` beneath
  whichever view is active, so it appears on both the resume and About views.

## About page content

The same information is presented in both frontends, grouped into five
sections. The first four describe what exists; the fifth is explicitly labeled
as not yet built.

**Backend**

- .NET 10 / C#
- ASP.NET Core
- FastEndpoints, organized as vertical slices per feature
- Entity Framework Core with PostgreSQL; migrations apply on startup in hosted
  environments and carry the seed data
- xUnit endpoint tests

**Frontends**

- Blazor Web App using the `InteractiveAuto` render mode — server-rendered
  first, then WebAssembly once the runtime has downloaded
- React 19 with TypeScript, built by Vite and linted by oxlint
- Both call the same API and implement the same UX independently

**Platform and tooling**

- .NET Aspire for local orchestration of the database, API, and both UIs
- YARP reverse proxy fronting the API from the Blazor host
- OpenTelemetry instrumentation via the shared ServiceDefaults project, with
  an OTLP exporter
- Docker containers for PostgreSQL and pgAdmin in local development

**Azure deployment (implemented)**

- Azure Developer CLI (`azd`) drives provisioning and deployment from
  `azure.yaml`, which points at the Aspire AppHost; `azd` generates the
  infrastructure in memory rather than checking Bicep into the repo
- Azure Container Apps hosts three publicly-ingressed apps: `api`,
  `blazorapp`, and `react`
- Azure Container Registry holds the images `azd` builds
- The React app deploys as a production Vite bundle served by nginx, which
  reverse-proxies `/api/` to the API container so no API host is compiled into
  the bundle
- The database is an external managed PostgreSQL instance (Neon), supplied as
  a connection string held as a secret in the `azd` environment; the AppHost
  binds `resumedb` to it in publish mode instead of provisioning a container

**Not yet implemented**

- A CI/CD pipeline. Deployment is a manual `azd up` / `azd deploy`;
  `azd pipeline config` has not been run and no `azure-dev.yml` exists.
- Azure Monitor / Application Insights. The app emits OpenTelemetry, but the
  `UseAzureMonitor()` call in `Resume.ServiceDefaults` is still commented out,
  so nothing collects that telemetry once deployed.
- Managed Identity. Database access uses a connection string rather than
  workload identity.
- Azure Key Vault. Secrets live in the `azd` environment, not a vault.
- A custom domain with TLS. The apps answer on their generated Container Apps
  hostnames.
- Health probes outside development. `MapDefaultEndpoints` only maps the
  health and liveness endpoints when the environment is Development, so
  Container Apps has no probe to call.

The last group must be visually distinct — a heading that names it as
outstanding, plus a short sentence stating that these pieces are not deployed
today — so a reader cannot mistake the list for a description of running
infrastructure.

### Current UI and switch link

Below those sections, a line naming the frontend the visitor is on, and a link
to the other one:

- Blazor: "You are viewing the Blazor UI." → "View this resume in the React UI"
- React: "You are viewing the React UI." → "View this resume in the Blazor UI"

## Routing

### Blazor

A new `src/Resume.BlazorApp.Client/Pages/About.razor` with `@page "/about"`.
The existing `Router` in `Routes.razor` scans the client assembly, so the page
is discovered with no registration change. It inherits `MainLayout`, and
therefore the footer, by default.

### React

The React app has no router. Rather than add `react-router-dom` for a single
page, `App.tsx` keeps a `route` state initialized from `window.location.hash`
and updated by a `hashchange` listener; `#/about` renders the About view and
anything else renders the resume. The footer link is a plain
`<a href="#/about">`.

Hash routing keeps the deployed nginx configuration untouched. The template's
`try_files $uri $uri/ /index.html` already serves the SPA shell for deep paths,
so path routing would also work, but a hash needs no server involvement at all
and cannot collide with the `/api/` or `/switch-ui` locations.

## Cross-UI switch link

Neither frontend can hardcode the other's address, and neither can bake it in
at build time: the React bundle is compiled in a Docker build long before
provisioning knows any Container Apps hostname. This is the same constraint
that pushed the API URL out of the bundle and into an nginx proxy.

The design applies that existing solution symmetrically. **Each UI links to
`/switch-ui` on its own origin, and its own host issues a redirect to the
other UI.** No cross-origin URL reaches the browser as markup.

### AppHost wiring

`src/Resume.AppHost/Program.cs` supplies each host the other's endpoint. The
`react` resource's endpoints are created inside the existing publish/dev
branch, so the value it needs is set within each branch; `blazorapp`'s endpoint
exists unconditionally, so its variable is set once after the branch, beside
the existing `WithExternalHttpEndpoints()` call.

```csharp
if (builder.ExecutionContext.IsPublishMode)
{
    react.WithHttpEndpoint(targetPort: 80)
        .WithEnvironment("API_URL", api.GetEndpoint("https"))
        // Substituted into the nginx template's /switch-ui redirect.
        .WithEnvironment("BLAZOR_URL", blazorApp.GetEndpoint("https"))
        .WithEnvironment("NODE_ENV", "production")
        .PublishAsDockerFile();
}
else
{
    react.WithNpmPackageInstallation()
        .WithHttpEndpoint(env: "PORT", port: 5173)
        .WithEnvironment("VITE_API_URL", api.GetEndpoint("https"))
        // No nginx in dev, so the bundle reads this one directly.
        .WithEnvironment("VITE_BLAZOR_URL", blazorApp.GetEndpoint("https"));
}

react.WithExternalHttpEndpoints();

// The Blazor host resolves this at request time in its /switch-ui endpoint,
// so it is safe for it to be a plain configuration value.
blazorApp.WithEnvironment("ReactAppUrl", react.GetEndpoint("http"));
```

These references are mutual: `blazorapp` reads `react`'s endpoint and `react`
reads `blazorapp`'s. Neither adds startup ordering — only `WaitFor` does — so
local orchestration is unaffected. Whether `azd` tolerates the cycle when it
generates Container Apps infrastructure is **the one unverified assumption in
this design**, and the implementation must check it before anything else is
built (see Testing).

If `azd` rejects the cycle, the fallback is to drop the AppHost references and
set both URLs as plain `azd` environment variables after the first
provisioning, accepting that the first `azd up` of a fresh environment leaves
the switch links dead until a second `azd deploy`.

### React side

The deployed nginx serves the redirect. `nginx.conf.template` gains a location
beside the existing `/api/` block:

```nginx
# Sends visitors to the Blazor UI without the bundle knowing its address.
location = /switch-ui {
    return 302 ${BLAZOR_URL};
}
```

`BLAZOR_URL` must be added to the Dockerfile's `NGINX_ENVSUBST_FILTER`, which
currently allows only `API_URL` and `NGINX_LOCAL_RESOLVERS`; a name outside
that filter is left unsubstituted and nginx fails to start.

Because the dev server has no nginx, the About page picks its target the same
way `src/Resume.React/src/api/client.ts` picks its API base — a build-time
value when one exists, the same-origin path otherwise:

```ts
// Dev gets the Blazor URL injected by the AppHost. In the deployed container
// it is unset, so the link goes same-origin to /switch-ui and nginx redirects.
const BLAZOR_URL = (import.meta.env.VITE_BLAZOR_URL as string | undefined) ?? '/switch-ui'
```

### Blazor side

`Routes` is rendered `@rendermode="InteractiveAuto"`, so every routable page
can execute in WebAssembly, where the server's `IConfiguration` is not
available. Injecting configuration into `About.razor` would therefore work
only until the WebAssembly runtime took over.

Instead, `src/Resume.BlazorApp/Program.cs` maps a redirect endpoint that
mirrors the nginx one:

```csharp
app.MapGet("/switch-ui", (IConfiguration configuration) =>
{
    var reactAppUrl = configuration["ReactAppUrl"];
    return string.IsNullOrWhiteSpace(reactAppUrl)
        ? Results.NotFound()
        : Results.Redirect(reactAppUrl);
});
```

`About.razor` links to `/switch-ui`, a plain anchor that behaves identically
under server rendering and WebAssembly.

If `ReactAppUrl` is unset — for instance if the Blazor app is run directly
rather than through the AppHost — the endpoint returns 404. The About page
still renders in full; only the switch link leads to a not-found response.
That is acceptable for a demo app and keeps the wiring to a single line.

## Files touched

| File | Change |
|---|---|
| `src/Resume.AppHost/Program.cs` | Cross-UI endpoint environment variables |
| `src/Resume.BlazorApp/Program.cs` | `/switch-ui` redirect endpoint |
| `src/Resume.BlazorApp/wwwroot/app.css` | Footer and About page styles |
| `src/Resume.BlazorApp.Client/Layout/MainLayout.razor` | Render the footer |
| `src/Resume.BlazorApp.Client/Pages/About.razor` | New page |
| `src/Resume.React/nginx.conf.template` | `/switch-ui` redirect location |
| `src/Resume.React/Dockerfile` | Add `BLAZOR_URL` to the envsubst filter |
| `src/Resume.React/src/App.tsx` | Hash route, render footer |
| `src/Resume.React/src/App.css` | Footer and About page styles |
| `src/Resume.React/src/components/ResumeFooter.tsx` | New component |
| `src/Resume.React/src/pages/About.tsx` | New view |
| `README.md` | Note the About page and the switch-link plumbing |

## Testing

The project has no frontend test infrastructure, and adding it remains out of
scope. Verification is build, lint, manifest inspection, and manual use.

**First, before building anything else** — confirm the mutual AppHost
references survive publish. Make the `Program.cs` change alone and generate the
publish manifest:

```bash
dotnet run --project src/Resume.AppHost -- --publisher manifest --output-path manifest.json
```

The manifest must contain both `BLAZOR_URL` on `react` and `ReactAppUrl` on
`blazorapp`, each as a resolved endpoint placeholder, with no cycle error. If
this fails, take the fallback described above and revise this spec before
continuing.

Then:

- `dotnet build Resume.sln` succeeds.
- `dotnet test Resume.sln` still passes — the API is untouched, so this is a
  regression check.
- `npm run lint` and `npx tsc -b` succeed in `src/Resume.React`.
- `docker build src/Resume.React` succeeds, and running the image with
  `API_URL` and `BLAZOR_URL` set serves the SPA and answers `/switch-ui` with a
  302 to the `BLAZOR_URL` value.
- Running `dotnet run --project src/Resume.AppHost`, then confirming by hand:
  the footer appears on every page of both UIs with the header's blue and the
  centered stacked layout; `/about` and `#/about` render the About page; the
  Blazor switch link lands on the React app and the React switch link lands on
  the Blazor app.

## Open assumptions

- `azd` accepts the mutual endpoint references between `blazorapp` and
  `react`. This is checked first, and the design has a stated fallback.
- The copyright holder is "Jeffrey M. Proulx", matching the seeded resume, and
  the year is written as a literal `2026` rather than computed at runtime.
- The footer shows the same links on every page, including the About page
  itself. The switch link lives in the About page body, not in the footer.
- The About content is maintained by hand in two places. Divergence between the
  Blazor and React copies is a known cost of the project's no-shared-components
  rule, not a defect to design around.
- The "Not yet implemented" list is accurate as of this date and will drift as
  that work lands. Keeping it current is a maintenance cost accepted in
  exchange for the page being useful.
