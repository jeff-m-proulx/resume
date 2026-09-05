# About Page and Site Footer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give both frontends a blue footer linking to a new About page that describes the technology stack, including the Azure deployment work still outstanding, and lets a visitor switch to the other UI.

**Architecture:** Each frontend implements the footer and the About page independently — this repo has two parallel UIs with no shared component library, by design. The cross-UI switch link is a same-origin `/switch-ui` path on each host that redirects to the other UI: a minimal-API endpoint on the Blazor host, an nginx `return 302` in the deployed React container. Neither UI ever has the other's address in its markup, because the React bundle is built in Docker long before provisioning knows any Container Apps hostname.

**Tech Stack:** .NET 10, Blazor Web App (`InteractiveAuto`), React 19 + TypeScript + Vite, .NET Aspire 13.5.2, nginx, Docker, `azd`.

**Spec:** `docs/superpowers/specs/2026-09-05-about-page-and-footer-design.md`

## Global Constraints

- **No new npm or NuGet packages.** In particular, do not add `react-router-dom`; the React app routes on `window.location.hash`.
- **No shared component library between the two frontends.** The About copy and the footer CSS are deliberately duplicated. Keep the two copies textually identical so a future diff is meaningful.
- **Header blue is `#1f3b57`.** Secondary text on blue is `#d9e3ec`. Both values already appear in `.resume-header` in each stylesheet; the footer reuses them exactly.
- **Copyright line is exactly `© 2026 Jeffrey M. Proulx`** — a literal year, not computed at runtime.
- **Class naming follows the existing BEM-ish convention** (`.resume-footer`, `.resume-footer__links`, `.about-section--planned`).
- **No frontend test framework exists and none is being added** (an explicit non-goal in both specs). Verification for UI work is build + lint + typecheck + the stated manual checks. Every task still ends with a run-it-and-observe step before its commit; where that step is a command with expected output, paste the real output into the commit or the review, not a claim that it passed.
- **The two stylesheets must stay in sync.** `src/Resume.BlazorApp/wwwroot/app.css` and `src/Resume.React/src/App.css` receive byte-identical rule blocks in Tasks 4 and 5.

---

## Task 1: Cross-UI endpoint wiring in the AppHost

This task is first because it carries the design's only unverified assumption: `blazorapp` and `react` end up referencing each other's endpoints, and if `azd` rejects that cycle the rest of the plan needs a different switch-link mechanism. Verify before building any UI.

**Files:**
- Modify: `src/Resume.AppHost/Program.cs:27-48`

**Interfaces:**
- Consumes: nothing.
- Produces: environment variable `ReactAppUrl` on the `blazorapp` resource (consumed by Task 2), `BLAZOR_URL` on the deployed `react` container (consumed by Task 3), and `VITE_BLAZOR_URL` on the `react` dev server (consumed by Task 5).

- [ ] **Step 1: Capture the baseline manifest and confirm the variables are absent**

This is the failing-test equivalent for a configuration change: prove the thing you are about to add is not already there.

```bash
dotnet run --project src/Resume.AppHost -- \
  --operation publish --publisher manifest --output-path ./manifest.json
grep -E "BLAZOR_URL|ReactAppUrl" manifest.json
```

Expected: the command writes `manifest.json` and prints a startup banner ending in "Press CTRL+C to stop the AppHost and exit." — it exits on its own once the manifest is written, so do not interrupt it. `grep` finds nothing and exits 1.

Note: `--operation publish` is required. Passing only `--publisher manifest` silently launches the AppHost normally and writes no manifest.

- [ ] **Step 2: Add the environment variables**

In `src/Resume.AppHost/Program.cs`, inside the publish branch, add `BLAZOR_URL` after the existing `API_URL` line:

```csharp
if (builder.ExecutionContext.IsPublishMode)
{
    // Deployed as a static bundle behind nginx (see the React Dockerfile).
    // API_URL feeds the nginx template's proxy_pass, so the browser bundle
    // ships without an API host compiled into it.
    react.WithHttpEndpoint(targetPort: 80)
        .WithEnvironment("API_URL", api.GetEndpoint("https"))
        // Same reasoning for the Blazor app's address: it feeds the nginx
        // template's /switch-ui redirect rather than the bundle.
        .WithEnvironment("BLAZOR_URL", blazorApp.GetEndpoint("https"))
        .WithEnvironment("NODE_ENV", "production")
        .PublishAsDockerFile();
}
```

In the dev branch, add `VITE_BLAZOR_URL` after the existing `VITE_API_URL` line:

```csharp
else
{
    // Local dev runs the Vite dev server: PORT tells Vite where to listen and
    // VITE_API_URL is read as the dev server starts.
    react.WithNpmPackageInstallation()
        .WithHttpEndpoint(env: "PORT", port: 5173)
        .WithEnvironment("VITE_API_URL", api.GetEndpoint("https"))
        // There is no nginx in dev, so the bundle reads this one directly.
        .WithEnvironment("VITE_BLAZOR_URL", blazorApp.GetEndpoint("https"));
}
```

And after the existing `react.WithExternalHttpEndpoints();` line, add:

```csharp
// The Blazor host resolves this per request in its /switch-ui endpoint, so
// unlike the React side it needs no container-start substitution. Set after
// the branch because react's endpoint is created inside it.
blazorApp.WithEnvironment("ReactAppUrl", react.GetEndpoint("http"));
```

- [ ] **Step 3: Regenerate the manifest and confirm both variables resolve**

```bash
dotnet run --project src/Resume.AppHost -- \
  --operation publish --publisher manifest --output-path ./manifest.json
grep -E "BLAZOR_URL|ReactAppUrl" manifest.json
```

Expected — exactly these two lines, as endpoint placeholders rather than literal URLs:

```
        "ReactAppUrl": "{react.bindings.http.url}",
        "BLAZOR_URL": "{blazorapp.bindings.https.url}",
```

If the command instead fails with a circular-dependency error, **stop and report it** — the fallback is described in the spec's "AppHost wiring" section and the plan needs revising before Tasks 2, 3, and 5 proceed.

- [ ] **Step 4: Confirm `azd` can still generate infrastructure from the cycle**

The manifest proves Aspire accepts the mutual references; this proves `azd` can turn them into Bicep. `azd infra gen` writes files that then become the source of truth for `azd`, so this is a throwaway check — the generated directories must be deleted before committing.

```bash
azd infra gen --force
grep -rE "BLAZOR_URL|ReactAppUrl" infra/ src/*/manifests/ 2>/dev/null | head
```

Expected: the command completes without a dependency-cycle error, and the grep shows both names appearing in the generated Container Apps templates.

If `azd` instead prompts for a subscription or fails on authentication, the local `azd` environment has no subscription selected. Do not log in or select one just for this check — press Ctrl+C, note in the commit that this step was skipped for lack of an `azd` login, and continue. Step 3's manifest check is the load-bearing one; this step only moves the discovery of a cycle earlier than the next `azd up`.

Now delete the generated output — it must not be committed:

```bash
rm -rf infra/ src/Resume.Api/manifests/ src/Resume.BlazorApp/manifests/ src/Resume.React/manifests/
git status --short
```

Expected: `git status` shows only `src/Resume.AppHost/Program.cs` modified and `manifest.json` untracked. If any `infra/` or `manifests/` path is still listed, remove it before continuing.

- [ ] **Step 5: Confirm the dev-mode variable actually reaches the React resource**

```bash
dotnet run --project src/Resume.AppHost
```

Open the Aspire dashboard URL from the console, select the `react` resource, and open its **Environment** tab. Expected: `VITE_BLAZOR_URL` is listed with a value of `https://localhost:7113` (the Blazor app's dev address from its `launchSettings.json`). Also check the `blazorapp` resource's Environment tab for `ReactAppUrl` with a value of `http://localhost:5173`.

Stop the AppHost with Ctrl+C.

- [ ] **Step 6: Commit**

```bash
rm -f manifest.json
git add src/Resume.AppHost/Program.cs
git commit -m "Give each UI the other's endpoint from the AppHost"
```

---

## Task 2: `/switch-ui` redirect on the Blazor host

**Files:**
- Modify: `src/Resume.BlazorApp/Program.cs:57-59` (between `app.MapReverseProxy();` and `app.MapStaticAssets();`)

**Interfaces:**
- Consumes: the `ReactAppUrl` configuration value from Task 1.
- Produces: a `GET /switch-ui` route on the Blazor host that answers `302` with the React app's URL in the `Location` header, or `404` when `ReactAppUrl` is unset. Task 4's `About.razor` links to it.

- [ ] **Step 1: Confirm the route does not exist yet**

Start the AppHost in one terminal:

```bash
dotnet run --project src/Resume.AppHost
```

In another terminal:

```bash
curl -k -i https://localhost:7113/switch-ui
```

Expected: `HTTP/1.1 404 Not Found` — the request falls through to the Blazor `not-found` re-execution. Leave the AppHost running for Step 3.

- [ ] **Step 2: Add the endpoint**

In `src/Resume.BlazorApp/Program.cs`, immediately after `app.MapReverseProxy();`:

```csharp
// The About page's link to the React UI. About.razor renders under
// InteractiveAuto and so may execute in WebAssembly, where the server's
// configuration is not available -- the page links to this path and the host
// resolves the address here instead.
app.MapGet("/switch-ui", (IConfiguration configuration) =>
{
    var reactAppUrl = configuration["ReactAppUrl"];
    return string.IsNullOrWhiteSpace(reactAppUrl)
        ? Results.NotFound()
        : Results.Redirect(reactAppUrl);
});
```

- [ ] **Step 3: Verify the redirect**

Stop and restart the AppHost so the Blazor host rebuilds, then:

```bash
curl -k -i https://localhost:7113/switch-ui
```

Expected: `HTTP/1.1 302 Found` with a `Location: http://localhost:5173` header. Follow it in a browser and confirm the React resume loads.

Stop the AppHost.

- [ ] **Step 4: Confirm the solution still builds**

```bash
dotnet build Resume.sln
```

Expected: `Build succeeded`. Six pre-existing `CS9113: Parameter 'db' is unread` warnings in `Resume.Api` are expected and unrelated — do not fix them here.

- [ ] **Step 5: Commit**

```bash
git add src/Resume.BlazorApp/Program.cs
git commit -m "Redirect /switch-ui to the React UI from the Blazor host"
```

---

## Task 3: `/switch-ui` redirect in the deployed React container

**Files:**
- Modify: `src/Resume.React/nginx.conf.template`
- Modify: `src/Resume.React/Dockerfile:20` (the `NGINX_ENVSUBST_FILTER` line)

**Interfaces:**
- Consumes: the `BLAZOR_URL` environment variable from Task 1.
- Produces: a `GET /switch-ui` route in the deployed React container answering `302` to the Blazor app. Task 5's `About.tsx` links to it whenever `VITE_BLAZOR_URL` is unset.

- [ ] **Step 1: Confirm the current image does not serve the route**

```bash
docker build -t resume-react-check src/Resume.React
docker run --rm -d -p 8080:80 -e API_URL=http://example.invalid -e BLAZOR_URL=http://example.test --name resume-react-check resume-react-check
curl -i http://localhost:8080/switch-ui
```

Expected: `HTTP/1.1 200 OK` serving the SPA shell — nginx's `try_files` falls back to `index.html`, so the path is not a redirect yet.

```bash
docker stop resume-react-check
```

- [ ] **Step 2: Add the redirect location**

In `src/Resume.React/nginx.conf.template`, add this block after the `location /api/ { ... }` block:

```nginx
    # The About page's link to the Blazor UI. Kept out of the bundle for the
    # same reason as the API host: the image is built long before provisioning
    # knows the Blazor app's address, so the AppHost supplies BLAZOR_URL at
    # container start and the redirect happens here.
    location = /switch-ui {
        return 302 ${BLAZOR_URL};
    }
```

`location =` is an exact match, so it takes priority over the `location /` prefix match that would otherwise serve the SPA shell.

- [ ] **Step 3: Allow `BLAZOR_URL` through the envsubst filter**

In `src/Resume.React/Dockerfile`, change:

```dockerfile
ENV NGINX_ENVSUBST_FILTER="^(API_URL|NGINX_LOCAL_RESOLVERS)"
```

to:

```dockerfile
ENV NGINX_ENVSUBST_FILTER="^(API_URL|BLAZOR_URL|NGINX_LOCAL_RESOLVERS)"
```

Without this the template's `${BLAZOR_URL}` is left literal, and nginx fails to start with a config parse error. Also update the comment two lines above it, which currently says "these two names":

```dockerfile
# The nginx entrypoint runs envsubst over /etc/nginx/templates/*.template. The
# filter limits substitution to these three names so nginx's own runtime
# variables ($uri, $proxy_host, $api_upstream) pass through untouched.
```

- [ ] **Step 4: Rebuild and verify the redirect**

```bash
docker build -t resume-react-check src/Resume.React
docker run --rm -d -p 8080:80 -e API_URL=http://example.invalid -e BLAZOR_URL=https://blazor.example.test --name resume-react-check resume-react-check
curl -i http://localhost:8080/switch-ui
```

Expected: `HTTP/1.1 302 Moved Temporarily` with `Location: https://blazor.example.test`.

Confirm the SPA and its deep routes still work — the new exact-match location must not have shadowed anything:

```bash
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8080/
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8080/anything/deep
```

Expected: `200` for both.

- [ ] **Step 5: Clean up the check container and image**

```bash
docker stop resume-react-check
docker rmi resume-react-check
```

- [ ] **Step 6: Commit**

```bash
git add src/Resume.React/nginx.conf.template src/Resume.React/Dockerfile
git commit -m "Redirect /switch-ui to the Blazor UI from the React container"
```

---

## Task 4: Blazor footer and About page

**Files:**
- Create: `src/Resume.BlazorApp.Client/Components/ResumeFooter.razor`
- Create: `src/Resume.BlazorApp.Client/Pages/About.razor`
- Modify: `src/Resume.BlazorApp.Client/Layout/MainLayout.razor`
- Modify: `src/Resume.BlazorApp/wwwroot/app.css` (append at the end, before the closing `@media (max-width: 640px)` block)

**Interfaces:**
- Consumes: the `GET /switch-ui` route from Task 2.
- Produces: the CSS rule blocks for `.resume-footer*` and `.about-*`, which Task 5 copies verbatim into `src/Resume.React/src/App.css`, and the About page copy, which Task 5 reproduces in JSX.

**Note on file layout:** the spec's file table put the footer markup directly in `MainLayout.razor`. This task puts it in its own `ResumeFooter.razor` component instead, mirroring the React side's `ResumeFooter.tsx` and keeping the layout file to three lines. Same rendered output.

- [ ] **Step 1: Add the footer and About styles**

Append to `src/Resume.BlazorApp/wwwroot/app.css`, immediately before the closing `@media (max-width: 640px)` block at the end of the file:

```css
/* Bookends the sticky header: same blue, same radius, but in the page flow
   rather than pinned -- a second fixed band leaves too little room to read on
   a short viewport. The wrapper repeats .resume-page's width and side padding
   so the footer's edges line up with the content above it. */
.resume-footer-container {
    max-width: 800px;
    margin: 0 auto;
    padding: 0 1rem 1rem;
}

.resume-footer {
    background: #1f3b57;
    border-radius: 8px;
    padding: 1.5rem 1rem;
    margin-top: 1.5rem;
    text-align: center;
}

/* A flex row with a gap, holding one link today: a second or third needs no
   layout change. */
.resume-footer__links {
    display: flex;
    flex-wrap: wrap;
    justify-content: center;
    gap: 1.5rem;
}

.resume-footer__link {
    color: #ffffff;
    text-decoration: none;
    font-size: 1rem;
}

.resume-footer__link:hover,
.resume-footer__link:focus-visible {
    text-decoration: underline;
}

.resume-footer__link:focus-visible {
    outline: 2px solid #ffffff;
    outline-offset: 2px;
}

.resume-footer__copyright {
    margin: 1.75rem 0 0;
    font-size: 0.8rem;
    color: #d9e3ec;
}

.about-page h1 {
    margin: 0 0 0.5rem;
    font-size: 2rem;
    color: #1f3b57;
}

.about-page__intro {
    margin: 0 0 1rem;
}

.about-page__back {
    display: inline-block;
    margin-bottom: 1.5rem;
    color: #1f3b57;
}

.about-section {
    margin-bottom: 1.5rem;
}

.about-section h2 {
    margin: 0 0 0.5rem;
    font-size: 1.15rem;
    color: #1f3b57;
}

.about-section ul {
    margin: 0;
    padding-left: 1.25rem;
}

.about-section li {
    margin-bottom: 0.35rem;
}

/* Amber, matching the demo banner, so nobody reads the outstanding work as a
   description of running infrastructure. */
.about-section--planned {
    background: #fff8c5;
    border: 1px solid #d4a72c;
    border-radius: 6px;
    padding: 1rem;
}

.about-section--planned p {
    margin: 0 0 0.75rem;
}

.about-page__switch {
    margin-top: 1.5rem;
    padding-top: 1rem;
    border-top: 1px solid #d0d7de;
}

.about-page__switch p {
    margin: 0 0 0.5rem;
}
```

- [ ] **Step 2: Create the footer component**

Create `src/Resume.BlazorApp.Client/Components/ResumeFooter.razor`:

```razor
<div class="resume-footer-container">
    <footer class="resume-footer">
        <nav class="resume-footer__links">
            <a class="resume-footer__link" href="/about">About</a>
        </nav>
        <p class="resume-footer__copyright">&copy; 2026 Jeffrey M. Proulx</p>
    </footer>
</div>
```

- [ ] **Step 3: Render it from the layout**

Replace the contents of `src/Resume.BlazorApp.Client/Layout/MainLayout.razor` with:

```razor
@inherits LayoutComponentBase

@Body

<ResumeFooter />

<div id="blazor-error-ui" data-nosnippet>
    An unhandled error has occurred.
    <a href="." class="reload">Reload</a>
    <span class="dismiss">🗙</span>
</div>
```

`ResumeFooter` needs no `@using`: `_Imports.razor` already pulls in `Resume.BlazorApp.Client.Components`.

- [ ] **Step 4: Create the About page**

Create `src/Resume.BlazorApp.Client/Pages/About.razor`:

```razor
@page "/about"

<PageTitle>About</PageTitle>

<div class="resume-page about-page">
    <h1>About this application</h1>
    <p class="about-page__intro">
        A personal resume site built to run two frontend stacks against one
        backend. Whichever UI you are viewing, the content below the header is
        the same data, served from PostgreSQL through the same API.
    </p>
    <a class="about-page__back" href="/">Back to the resume</a>

    <section class="about-section">
        <h2>Backend</h2>
        <ul>
            <li>.NET 10 and C#</li>
            <li>ASP.NET Core</li>
            <li>FastEndpoints, organized as a vertical slice per feature</li>
            <li>Entity Framework Core with PostgreSQL; migrations apply on startup in hosted environments and carry the seed data</li>
            <li>xUnit endpoint tests</li>
        </ul>
    </section>

    <section class="about-section">
        <h2>Frontends</h2>
        <ul>
            <li>Blazor Web App in the InteractiveAuto render mode — server-rendered first, then WebAssembly once the runtime has downloaded</li>
            <li>React 19 with TypeScript, built by Vite and linted by oxlint</li>
            <li>Both call the same API and implement the same experience independently, with no shared component library</li>
        </ul>
    </section>

    <section class="about-section">
        <h2>Platform and tooling</h2>
        <ul>
            <li>.NET Aspire orchestrates the database, the API, and both UIs for local development</li>
            <li>YARP reverse-proxies the API behind the Blazor host</li>
            <li>OpenTelemetry instrumentation with an OTLP exporter, shared through the ServiceDefaults project</li>
            <li>Docker containers for PostgreSQL and pgAdmin locally</li>
        </ul>
    </section>

    <section class="about-section">
        <h2>Azure deployment</h2>
        <ul>
            <li>The Azure Developer CLI (azd) provisions and deploys from azure.yaml, which points at the Aspire AppHost; the infrastructure is generated on demand rather than checked into the repository</li>
            <li>Azure Container Apps hosts three publicly reachable apps: the API, the Blazor site, and the React site</li>
            <li>Azure Container Registry holds the images azd builds</li>
            <li>The React app ships as a production Vite bundle served by nginx, which reverse-proxies /api/ to the API so no API address is compiled into the bundle</li>
            <li>The database is an external managed PostgreSQL instance (Neon), supplied as a connection string held as a secret in the azd environment</li>
        </ul>
    </section>

    <section class="about-section about-section--planned">
        <h2>Not yet implemented</h2>
        <p>These pieces of the deployment story are designed but not built — the list is honest about where the project actually stands.</p>
        <ul>
            <li>A CI/CD pipeline. Deploying is a manual azd up or azd deploy; no pipeline configuration exists.</li>
            <li>Azure Monitor and Application Insights. The app emits OpenTelemetry, but the Azure Monitor exporter is still commented out, so nothing collects that telemetry once deployed.</li>
            <li>Managed Identity. Database access uses a connection string rather than a workload identity.</li>
            <li>Azure Key Vault. Secrets live in the azd environment rather than a vault.</li>
            <li>A custom domain with TLS. The apps answer on their generated Container Apps hostnames.</li>
            <li>Health probes outside development. The health and liveness endpoints are only mapped in the Development environment, so Container Apps has nothing to probe.</li>
        </ul>
    </section>

    <div class="about-page__switch">
        <p>You are viewing the Blazor UI.</p>
        <a href="/switch-ui">View this resume in the React UI</a>
    </div>
</div>
```

- [ ] **Step 5: Build**

```bash
dotnet build Resume.sln
```

Expected: `Build succeeded`, with only the six pre-existing `CS9113` warnings from `Resume.Api`.

- [ ] **Step 6: Verify in the browser**

```bash
dotnet run --project src/Resume.AppHost
```

Open `https://localhost:7113` and confirm all of the following:

1. The footer appears below the resume: blue band matching the header, "About" centered on top, `© 2026 Jeffrey M. Proulx` centered beneath it in smaller, lighter text, with clear space between the two.
2. The footer's left and right edges line up with the sticky header's.
3. Clicking "About" navigates to `/about` and the About page renders with all six sections, the "Not yet implemented" block visually distinct in amber.
4. The footer is present on the About page too.
5. "Back to the resume" returns to `/`.
6. "View this resume in the React UI" lands on the React app at `http://localhost:5173`.
7. Narrow the window below 640px and confirm the footer still reads well and does not overflow horizontally.

Stop the AppHost.

- [ ] **Step 7: Commit**

```bash
git add src/Resume.BlazorApp/wwwroot/app.css \
        src/Resume.BlazorApp.Client/Components/ResumeFooter.razor \
        src/Resume.BlazorApp.Client/Pages/About.razor \
        src/Resume.BlazorApp.Client/Layout/MainLayout.razor
git commit -m "Add the footer and About page to the Blazor UI"
```

---

## Task 5: React footer and About page

`App.tsx` currently holds the whole resume view — data loading, admin mode, the sticky-header observer, and all the markup. Adding a second view to it would push it past what can be read at a glance, so this task first moves the existing view into `pages/ResumePage.tsx` unchanged, leaving `App.tsx` as a small router. That mirrors the Blazor side, where `ResumePage.razor` and `About.razor` sit under a router.

**Files:**
- Create: `src/Resume.React/src/pages/ResumePage.tsx` (the current contents of `App.tsx`, moved)
- Create: `src/Resume.React/src/pages/About.tsx`
- Create: `src/Resume.React/src/components/ResumeFooter.tsx`
- Modify: `src/Resume.React/src/App.tsx` (reduced to routing)
- Modify: `src/Resume.React/src/App.css`

**Interfaces:**
- Consumes: the `VITE_BLAZOR_URL` variable from Task 1, the `/switch-ui` nginx redirect from Task 3, and the CSS rule blocks written in Task 4.
- Produces: nothing later tasks depend on.

- [ ] **Step 1: Copy the styles from the Blazor stylesheet**

Open `src/Resume.BlazorApp/wwwroot/app.css` and copy every rule from the `.resume-footer-container` comment block through the `.about-page__switch p` rule — the block Task 4 added — into `src/Resume.React/src/App.css`, before that file's trailing `@media (max-width: 640px)` block. Copy it verbatim, comments included; the two stylesheets are kept identical on purpose, so a future diff between them means something.

Then verify they match:

```bash
grep -c "resume-footer\|about-section\|about-page" src/Resume.BlazorApp/wwwroot/app.css src/Resume.React/src/App.css
```

Expected: both files report the same count.

- [ ] **Step 2: Move the resume view into its own module**

Create `src/Resume.React/src/pages/ResumePage.tsx` containing the current contents of `src/Resume.React/src/App.tsx` with exactly four changes:

1. Fix the import paths for the one-level-deeper location: `'./api/client'` becomes `'../api/client'`, `'./types'` becomes `'../types'`, and each `'./components/X'` becomes `'../components/X'`.
2. Delete the `import './App.css'` line — `App.tsx` keeps that import.
3. Rename the component: `function App()` becomes `export function ResumePage()`.
4. Delete the trailing `export default App` line.

Nothing else changes: the data loading, the sticky-header `ResizeObserver`, admin mode, and all the markup move across untouched.

- [ ] **Step 3: Create the footer component**

Create `src/Resume.React/src/components/ResumeFooter.tsx`:

```tsx
export function ResumeFooter() {
  return (
    <div className="resume-footer-container">
      <footer className="resume-footer">
        <nav className="resume-footer__links">
          <a className="resume-footer__link" href="#/about">
            About
          </a>
        </nav>
        <p className="resume-footer__copyright">© 2026 Jeffrey M. Proulx</p>
      </footer>
    </div>
  )
}
```

- [ ] **Step 4: Create the About view**

Create `src/Resume.React/src/pages/About.tsx`. The prose matches `About.razor` word for word:

```tsx
// Dev gets the Blazor app's address injected by the AppHost and links straight
// to it. In the deployed container it is unset, so the link goes same-origin to
// /switch-ui and nginx redirects -- the address is not known when the bundle is
// built, exactly as with the API URL in api/client.ts.
const BLAZOR_URL = (import.meta.env.VITE_BLAZOR_URL as string | undefined) ?? '/switch-ui'

export function About() {
  return (
    <div className="resume-page about-page">
      <h1>About this application</h1>
      <p className="about-page__intro">
        A personal resume site built to run two frontend stacks against one backend. Whichever UI
        you are viewing, the content below the header is the same data, served from PostgreSQL
        through the same API.
      </p>
      <a className="about-page__back" href="#/">
        Back to the resume
      </a>

      <section className="about-section">
        <h2>Backend</h2>
        <ul>
          <li>.NET 10 and C#</li>
          <li>ASP.NET Core</li>
          <li>FastEndpoints, organized as a vertical slice per feature</li>
          <li>
            Entity Framework Core with PostgreSQL; migrations apply on startup in hosted
            environments and carry the seed data
          </li>
          <li>xUnit endpoint tests</li>
        </ul>
      </section>

      <section className="about-section">
        <h2>Frontends</h2>
        <ul>
          <li>
            Blazor Web App in the InteractiveAuto render mode — server-rendered first, then
            WebAssembly once the runtime has downloaded
          </li>
          <li>React 19 with TypeScript, built by Vite and linted by oxlint</li>
          <li>
            Both call the same API and implement the same experience independently, with no shared
            component library
          </li>
        </ul>
      </section>

      <section className="about-section">
        <h2>Platform and tooling</h2>
        <ul>
          <li>
            .NET Aspire orchestrates the database, the API, and both UIs for local development
          </li>
          <li>YARP reverse-proxies the API behind the Blazor host</li>
          <li>
            OpenTelemetry instrumentation with an OTLP exporter, shared through the ServiceDefaults
            project
          </li>
          <li>Docker containers for PostgreSQL and pgAdmin locally</li>
        </ul>
      </section>

      <section className="about-section">
        <h2>Azure deployment</h2>
        <ul>
          <li>
            The Azure Developer CLI (azd) provisions and deploys from azure.yaml, which points at
            the Aspire AppHost; the infrastructure is generated on demand rather than checked into
            the repository
          </li>
          <li>
            Azure Container Apps hosts three publicly reachable apps: the API, the Blazor site, and
            the React site
          </li>
          <li>Azure Container Registry holds the images azd builds</li>
          <li>
            The React app ships as a production Vite bundle served by nginx, which reverse-proxies
            /api/ to the API so no API address is compiled into the bundle
          </li>
          <li>
            The database is an external managed PostgreSQL instance (Neon), supplied as a connection
            string held as a secret in the azd environment
          </li>
        </ul>
      </section>

      <section className="about-section about-section--planned">
        <h2>Not yet implemented</h2>
        <p>
          These pieces of the deployment story are designed but not built — the list is honest about
          where the project actually stands.
        </p>
        <ul>
          <li>
            A CI/CD pipeline. Deploying is a manual azd up or azd deploy; no pipeline configuration
            exists.
          </li>
          <li>
            Azure Monitor and Application Insights. The app emits OpenTelemetry, but the Azure
            Monitor exporter is still commented out, so nothing collects that telemetry once
            deployed.
          </li>
          <li>
            Managed Identity. Database access uses a connection string rather than a workload
            identity.
          </li>
          <li>Azure Key Vault. Secrets live in the azd environment rather than a vault.</li>
          <li>
            A custom domain with TLS. The apps answer on their generated Container Apps hostnames.
          </li>
          <li>
            Health probes outside development. The health and liveness endpoints are only mapped in
            the Development environment, so Container Apps has nothing to probe.
          </li>
        </ul>
      </section>

      <div className="about-page__switch">
        <p>You are viewing the React UI.</p>
        <a href={BLAZOR_URL}>View this resume in the Blazor UI</a>
      </div>
    </div>
  )
}
```

- [ ] **Step 5: Reduce `App.tsx` to routing**

Replace the entire contents of `src/Resume.React/src/App.tsx` with:

```tsx
import { useEffect, useState } from 'react'
import { ResumePage } from './pages/ResumePage'
import { About } from './pages/About'
import { ResumeFooter } from './components/ResumeFooter'
import './App.css'

const ABOUT_ROUTE = '#/about'

// A hash rather than a path: nginx serves this bundle in production, and a hash
// needs no server involvement at all -- no rewrite rules, and no chance of
// colliding with the /api/ or /switch-ui locations.
function App() {
  const [route, setRoute] = useState(() => window.location.hash)

  useEffect(() => {
    const handleHashChange = () => setRoute(window.location.hash)
    window.addEventListener('hashchange', handleHashChange)
    return () => window.removeEventListener('hashchange', handleHashChange)
  }, [])

  return (
    <>
      {route === ABOUT_ROUTE ? <About /> : <ResumePage />}
      <ResumeFooter />
    </>
  )
}

export default App
```

- [ ] **Step 6: Typecheck and lint**

```bash
cd src/Resume.React
npx tsc -b
npm run lint
```

Expected: both complete with no errors. `tsc` catches a missed import path from the Step 2 move; oxlint catches a hook used conditionally.

If `tsc` reports that `import.meta.env.VITE_BLAZOR_URL` has no type, stop and report it — `VITE_API_URL` is read the same way in `src/Resume.React/src/api/client.ts` without extra declarations, so a new error there means the move in Step 2 changed something it should not have.

- [ ] **Step 7: Verify in the browser**

```bash
cd ../..
dotnet run --project src/Resume.AppHost
```

Open `http://localhost:5173` and confirm:

1. The resume renders exactly as before the refactor — sections and subsections expand, Admin Mode opens the edit forms, and the sticky section headers still dock beneath the page header.
2. The footer appears below the resume and looks identical to the Blazor one: blue band, centered "About", smaller `© 2026 Jeffrey M. Proulx` beneath it.
3. Clicking "About" switches to the About view and the browser URL ends in `#/about`.
4. Browser Back returns to the resume — the `hashchange` listener must re-render it without a reload.
5. "Back to the resume" returns to the resume view.
6. "View this resume in the Blazor UI" lands on `https://localhost:7113`.
7. Open both UIs side by side on `/about` and `#/about` and confirm the copy and layout match.

Stop the AppHost.

- [ ] **Step 8: Commit**

```bash
git add src/Resume.React/src/App.tsx src/Resume.React/src/App.css \
        src/Resume.React/src/pages/ResumePage.tsx \
        src/Resume.React/src/pages/About.tsx \
        src/Resume.React/src/components/ResumeFooter.tsx
git commit -m "Add the footer and About page to the React UI"
```

---

## Task 6: Document the About page and the switch link

**Files:**
- Modify: `README.md` (the "Project layout" section at the end, and the "How the React app is built for Azure" section)

**Interfaces:**
- Consumes: everything above.
- Produces: nothing.

- [ ] **Step 1: Describe the switch link where the React build is explained**

In `README.md`, at the end of the "### How the React app is built for Azure" section, after the paragraph ending "the `API_URL` environment variable the AppHost supplies at container start.", add:

```markdown
The About page's link to the other UI works the same way. Each site links to
`/switch-ui` on its own origin and redirects from there — a minimal API
endpoint on the Blazor host, an nginx `return 302` in the React container —
so neither frontend needs the other's address at build time. The AppHost
supplies both addresses: `ReactAppUrl` to `blazorapp`, and `BLAZOR_URL` (or
`VITE_BLAZOR_URL` in local development) to `react`.
```

- [ ] **Step 2: List the new spec alongside the original**

Replace the "## Project layout" section at the end of `README.md` with:

```markdown
## Project layout

See `docs/superpowers/specs/2026-08-22-resume-app-design.md` for the full
design spec, and
`docs/superpowers/specs/2026-09-05-about-page-and-footer-design.md` for the
About page and site footer.
```

- [ ] **Step 3: Check the rendered result**

```bash
grep -n "switch-ui" README.md
```

Expected: one match, inside the React build section.

- [ ] **Step 4: Commit**

```bash
git add README.md
git commit -m "Document the About page and the cross-UI switch link"
```

---

## Final verification

Run after all six tasks, before opening a PR:

- [ ] `dotnet build Resume.sln` — succeeds, only the six pre-existing `CS9113` warnings.
- [ ] `dotnet test Resume.sln` — all tests pass. The API is untouched, so this is a regression check; report the actual pass count.
- [ ] `cd src/Resume.React && npx tsc -b && npm run lint` — both clean.
- [ ] `git status --short` — clean. In particular, no `manifest.json`, `infra/`, or `manifests/` left behind from Task 1.
- [ ] `dotnet run --project src/Resume.AppHost`, then walk both UIs once more: footer on every page, About page in both, and the switch link working in both directions.
