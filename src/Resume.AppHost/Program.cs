var builder = DistributedApplication.CreateBuilder(args);

// Local runs get the Postgres container; `azd` publish binds "resumedb" to an
// externally supplied connection string (Neon) instead.
IResourceBuilder<IResourceWithConnectionString> resumedb = builder.ExecutionContext.IsPublishMode
    ? builder.AddConnectionString("resumedb")
    : builder.AddPostgres("postgres")
        .WithDataVolume()
        .WithPgAdmin()
        .AddDatabase("resumedb");

// The API is reached directly by the React bundle's nginx proxy, and the Blazor
// site is the public entry point — both need ingress when deployed.
var api = builder.AddProject<Projects.Resume_Api>("api")
    .WithReference(resumedb)
    .WaitFor(resumedb)
    .WithExternalHttpEndpoints();

var blazorApp = builder.AddProject<Projects.Resume_BlazorApp>("blazorapp")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

var react = builder.AddNpmApp("react", "../Resume.React", "dev")
    .WaitFor(api);

if (builder.ExecutionContext.IsPublishMode)
{
    // Deployed as a static bundle behind nginx (see the React Dockerfile).
    // API_URL feeds the nginx template's proxy_pass, so the browser bundle
    // ships without an API host compiled into it.
    react.WithHttpEndpoint(targetPort: 80)
        .WithEnvironment("API_URL", api.GetEndpoint("https"))
        .WithEnvironment("NODE_ENV", "production")
        .PublishAsDockerFile();
}
else
{
    // Local dev runs the Vite dev server: PORT tells Vite where to listen and
    // VITE_API_URL is read as the dev server starts.
    react.WithNpmPackageInstallation()
        .WithHttpEndpoint(env: "PORT", port: 5173)
        .WithEnvironment("VITE_API_URL", api.GetEndpoint("https"));
}

// Marked after the branch: WithExternalHttpEndpoints only flags endpoints that
// already exist, and each branch adds its own.
react.WithExternalHttpEndpoints();

builder.Build().Run();
