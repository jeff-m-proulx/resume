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
            Entity Framework Core with PostgreSQL; migrations apply on startup and carry the seed
            data
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
