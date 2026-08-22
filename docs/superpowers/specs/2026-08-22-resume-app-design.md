# Resume Web Application — Design Spec

Date: 2026-08-22

## Summary

A .NET 10 / C# web application that presents a personal resume through two
independent frontends (Blazor and React), backed by a single FastEndpoints
API using vertical slice architecture, with data stored in a containerized
Postgres database, all orchestrated by .NET Aspire.

The API supports create/edit operations for demo purposes, but those writes
are always discarded — the database is never actually modified by the API.
This lets the app demonstrate a full CRUD-shaped admin experience without
allowing anyone to permanently change the resume's content.

## Goals

- Present personal information (excluding address), skills, experience, and
  education from a single source of truth (Postgres).
- Offer two parallel frontends — Blazor and React — implementing the same UX
  independently, to demonstrate both stacks against the same backend.
- Vertical-slice, FastEndpoints-based API with real reads and fake writes.
- Local development fully orchestrated through .NET Aspire (Postgres
  container, API, both frontends).
- Expandable/collapsible sections per resume category, with sticky section
  headers.
- A no-login "Admin Mode" UI toggle that exercises the create/edit API calls,
  visibly showing that changes do not persist.

## Non-goals (out of scope for this spec)

- Real authentication/authorization (no login system; admin mode is a
  client-side toggle only, per explicit decision).
- Delete endpoints (only create and edit, per the original request).
- Persisting admin edits in any way (in-memory session store, etc.) — writes
  are validated and then discarded entirely.
- Frontend automated tests or Aspire integration tests (unit tests for the
  API only, for this first pass).
- Production deployment/hosting configuration — this spec covers local
  development via Aspire only.
- Shared component library between Blazor and React — each implements the
  same UX independently.

## Architecture

```
                        ┌────────────────────┐
                        │   Resume.AppHost    │  (.NET Aspire orchestrator)
                        └─────────┬──────────┘
           ┌───────────────┬──────┴───────┬────────────────┐
           ▼               ▼              ▼                ▼
   ┌───────────────┐ ┌───────────┐ ┌──────────────┐ ┌──────────────┐
   │ Postgres       │ │Resume.Api │ │Resume.BlazorApp│ │Resume.React │
   │ (container)    │◄┤(FastEndpoints)◄┤(Blazor Web App)│ │(Vite/React) │
   └───────────────┘ └───────────┘ └──────────────┘ └──────────────┘
                            ▲                                │
                            └────────────── HTTP/JSON ────────┘
```

Both frontends are independent SPAs that talk to `Resume.Api` over
HTTP/JSON. Neither frontend talks to Postgres directly — only `Resume.Api`
(via `Resume.Data`) does.

## Solution / repo layout

Single solution (`Resume.sln`) at repo root, `src/`-organized:

- **`Resume.AppHost`** — .NET Aspire orchestrator. Registers the Postgres
  container, `Resume.Api`, `Resume.BlazorApp`, and `Resume.React` as Aspire
  resources with service discovery wired between them.
- **`Resume.ServiceDefaults`** — Aspire's standard shared telemetry,
  health-check, and resilience configuration, referenced by `Resume.Api` and
  `Resume.BlazorApp`.
- **`Resume.Contracts`** — class library with the request/response DTOs
  (records) shared between `Resume.Api` and `Resume.BlazorApp` via project
  reference. `Resume.React` hand-writes matching TypeScript interfaces (kept
  in sync manually for v1; FastEndpoints' built-in OpenAPI/Swagger output is
  available later for codegen if desired).
- **`Resume.Data`** — EF Core `DbContext`, entity classes, Npgsql
  configuration, migrations, and seed data.
- **`Resume.Api`** — FastEndpoints backend, vertical-slice organized under
  `Features/PersonalInfo`, `Features/Skills`, `Features/Experience`,
  `Features/Education`. Each feature folder contains its own
  Endpoint/Request/Response/Validator/Mapper.
- **`Resume.BlazorApp`** — Blazor Web App (Auto render mode: SSR + WASM
  interactivity), calls `Resume.Api` over HTTP via a configured `HttpClient`.
- **`Resume.React`** — Vite + React + TypeScript SPA, calls `Resume.Api`
  over HTTP, run as an Aspire Vite/npm app resource.
- **`tests/Resume.Api.Tests`** — xUnit tests covering the API vertical
  slices.

## Data model

Four entities in Postgres, managed by EF Core (`Resume.Data`). No address
field on `PersonalInfo`.

### PersonalInfo (singleton — one row for the one resume)

| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| FullName | string | required |
| Headline | string | e.g. "Senior Software Engineer" |
| Email | string | required |
| Phone | string | optional |
| Summary | string | short bio paragraph |
| LinkedInUrl | string | optional |
| GitHubUrl | string | optional |
| WebsiteUrl | string | optional |

### Skill

| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| Category | string | e.g. "Languages", "Frameworks", "Tools", "Cloud" |
| Name | string | required |
| SortOrder | int | controls display order within a category |

Grouped by `Category` in the UI. No proficiency rating (per decision).

### Experience

| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| Company | string | required |
| JobTitle | string | required |
| Location | string | optional |
| StartDate | DateOnly | required |
| EndDate | DateOnly? | null = current role |
| Highlights | text[] (Postgres array) | bullet points, via Npgsql's native array support |

### Education

| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| Institution | string | required |
| Degree | string | required |
| FieldOfStudy | string | optional |
| StartDate | DateOnly | required |
| EndDate | DateOnly? | null = in progress |
| Details | text[] (Postgres array) | optional honors/coursework bullets |

### Seed data

On first run, EF Core migrations create the schema and seed it with
realistic placeholder resume content (fake name, company, school, skills,
etc.) so the app is fully functional out of the box. Real resume content can
be substituted later by editing the seed data.

## API design (FastEndpoints, vertical slices, fake writes)

Each feature folder (`PersonalInfo`, `Skills`, `Experience`, `Education`)
follows the same shape:

- **`Get{Feature}Endpoint`** (`GET`) — a real read. Queries Postgres via
  `Resume.Data` and returns the actual stored data.
  - `GET /api/personal-info`
  - `GET /api/skills`
  - `GET /api/experience`
  - `GET /api/education`
- **`Create{Feature}Endpoint`** (`POST`) and **`Update{Feature}Endpoint`**
  (`PUT`) for `Skills`, `Experience`, and `Education`. `PersonalInfo` gets
  only `Update` (it's a singleton, nothing to "create").
- Each write endpoint has a FluentValidation validator (FastEndpoints'
  built-in support) enforcing required fields, string lengths, and date
  ordering (`EndDate >= StartDate` when present), so the API behaves like a
  real one for validation purposes.

### Fake-write behavior

On successful validation, the write endpoint **does not call
`SaveChangesAsync`** and never touches the database:

- **Create**: builds a response DTO from the submitted payload, assigning a
  new `Guid` as if the row had been inserted, and returns `200 OK` (or
  `201 Created`) with that DTO. Nothing is written to Postgres.
- **Update**: echoes back the submitted payload as the "updated" entity
  (using the `Id` from the route/request), returns `200 OK`. Nothing is
  written to Postgres.
- A subsequent `GET` always returns the original, unchanged data.

On validation failure, FastEndpoints returns its standard `400` with
validation error details — the demo still "feels" like a real API on the
failure path.

### Auth

No server-side authentication or authorization on the write endpoints. This
is an intentional decision, not an oversight: since writes are always
discarded and never reach the database, there is nothing to protect. Admin
mode is purely a client-side UI toggle in each frontend.

### API documentation

FastEndpoints' built-in Swagger/OpenAPI generation is enabled for
documentation purposes and as a future option for generating the React
TypeScript types.

## Frontend design (Blazor + React)

Both apps implement an identical UX independently — there is no shared
component library between Blazor and React; each is built natively in its
own stack.

### Layout

A single page with four collapsible sections, in this order: **Personal
Info, Skills, Experience, Education**.

- Each section is a card with a header row (title + expand/collapse
  chevron). The header uses `position: sticky; top: 0`, so when a section is
  expanded and its content is tall enough to scroll, the header stays
  pinned to the top of the viewport while scrolling through that section's
  content, then scrolls away normally once the user moves past the section.
- **Personal Info** starts expanded by default (it's short and functions as
  the page's header). Skills, Experience, and Education start collapsed.
- **Skills** are grouped visually by `Category` (e.g. chip/tag groups per
  category).
- **Experience** and **Education** are rendered as cards/timeline entries,
  each with its bullet list (`Highlights` / `Details`).

### Admin mode

A toggle control (e.g. "Admin Mode" button, top-right) with no login. When
enabled:

- Each section shows "Add" and "Edit" controls (a simple inline form or
  modal per entity type).
- Submitting a form calls the corresponding `Create`/`Update` endpoint on
  `Resume.Api`.
- On success, the UI shows a banner/toast: *"Saved — but this is a demo;
  changes are not actually persisted."* The UI then re-fetches the section's
  data from the API, which comes back unchanged, making it visible to the
  user that the edit did not actually take effect.

### Styling

Plain CSS in both apps (no heavy UI framework), kept visually consistent
(shared color palette, spacing, typography choices) but implemented
natively per stack — CSS files in `Resume.BlazorApp`, CSS-in-Vite in
`Resume.React`.

## Aspire orchestration

`Resume.AppHost` wires the whole system together:

- `builder.AddPostgres("postgres")` — a containerized Postgres instance with
  a `resumedb` database and a data volume so data survives restarts within
  a dev session.
- `Resume.Api` — added as a project resource, referencing the Postgres
  resource for its connection string via Aspire service discovery. EF Core
  migrations and seeding run automatically on startup in development.
- `Resume.BlazorApp` — added as a project resource, with `Resume.Api`'s
  endpoint URL injected so its `HttpClient` is auto-configured via service
  discovery.
- `Resume.React` — added via Aspire's Vite/npm app resource support, with
  `Resume.Api`'s URL injected as a `VITE_API_URL` environment variable,
  started via `npm run dev` under the Aspire dashboard.

Running `dotnet run` on `Resume.AppHost` brings up Postgres, the API, and
both frontends together, with unified logs/traces/health visible in the
Aspire dashboard.

## Testing

- `tests/Resume.Api.Tests` (xUnit) covers each vertical slice:
  - Validators reject invalid input (missing required fields, bad date
    ordering, etc.).
  - `Get` endpoints return seeded data correctly, using EF Core's InMemory
    provider (sufficient for these simple CRUD-shaped slices — no
    Postgres-specific query behavior is under test).
  - `Create`/`Update` endpoints return a well-formed success response for
    valid input, and a follow-up `Get` confirms the underlying data is
    unchanged (proving the fake-write behavior).
- No frontend tests and no Aspire integration tests in this first pass;
  can be added in a later iteration.

## Open assumptions

- "Vertical slice architecture" is applied within a single `Resume.Api`
  project (feature folders), not as separate assemblies per slice — this is
  the common interpretation of vertical slices with FastEndpoints and keeps
  the project count manageable.
- `PersonalInfo` is modeled as a singleton (single seeded row), since a
  resume describes exactly one person; the `Update` endpoint operates on
  that single row without an `Id` route parameter.
- Postgres runs only in a local dev container via Aspire for now — no
  external/managed Postgres or production deployment is in scope.
