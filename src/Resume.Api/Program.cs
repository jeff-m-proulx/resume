using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;
using Resume.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<ResumeDbContext>("resumedb");

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseCors();
app.UseFastEndpoints();
app.UseSwaggerGen();

// Migrations carry the HasData seed, so a freshly provisioned database needs
// them applied on startup — hosted environments included, not just local dev.
// The API tests run on the InMemory provider, where Migrate() is not valid.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ResumeDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
}

app.Run();

public partial class Program;
