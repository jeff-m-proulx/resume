var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var resumedb = postgres.AddDatabase("resumedb");

var api = builder.AddProject<Projects.Resume_Api>("api")
    .WithReference(resumedb)
    .WaitFor(resumedb);

var blazorApp = builder.AddProject<Projects.Resume_BlazorApp>("blazorapp")
    .WithReference(api)
    .WaitFor(api);

var react = builder.AddNpmApp("react", "../Resume.React", "dev")
    .WithHttpEndpoint(env: "PORT", port: 5173)
    .WithEnvironment("VITE_API_URL", api.GetEndpoint("https"))
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
