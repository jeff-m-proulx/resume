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

builder.Build().Run();
