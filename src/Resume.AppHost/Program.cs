var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var resumedb = postgres.AddDatabase("resumedb");

builder.Build().Run();
