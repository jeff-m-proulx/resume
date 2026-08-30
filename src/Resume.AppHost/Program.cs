var builder = DistributedApplication.CreateBuilder(args);

// Local runs get the Postgres container; `azd` publish binds "resumedb" to an
// externally supplied connection string (Neon) instead.
IResourceBuilder<IResourceWithConnectionString> resumedb = builder.ExecutionContext.IsPublishMode
    ? builder.AddConnectionString("resumedb")
    : builder.AddPostgres("postgres")
        .WithDataVolume()
        .WithPgAdmin()
        .AddDatabase("resumedb");

var api = builder.AddProject<Projects.Resume_Api>("api")
    .WithReference(resumedb)
    .WaitFor(resumedb);

var blazorApp = builder.AddProject<Projects.Resume_BlazorApp>("blazorapp")
    .WithReference(api)
    .WaitFor(api);

var react = builder.AddNpmApp("react", "../Resume.React", "dev")
    .WithNpmPackageInstallation()
    .WithHttpEndpoint(env: "PORT", port: 5173)
    .WithEnvironment("VITE_API_URL", api.GetEndpoint("https"))
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
