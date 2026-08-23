using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Resume.Data;

namespace Resume.Api.Tests;

public class ApiTestFixture : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Program.cs runs EF Core migrations when the environment is
        // Development, which WebApplicationFactory uses by default. Migrate()
        // is a relational-only operation and throws against the InMemory
        // provider, so the test host runs as "Testing" instead.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:resumedb", "Host=localhost;Database=test;Username=test;Password=test");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ResumeDbContext>>();

            // AddNpgsqlDbContext registers a *pooled* context, which also adds
            // internal singleton/scoped services (IDbContextPool<T>,
            // IScopedDbContextLease<T>, etc.) keyed to ResumeDbContext. Those
            // must go too, or the container fails validation because they end
            // up depending on the scoped DbContextOptions<T> registered below.
            var pooledDescriptors = services
                .Where(d =>
                    d.ServiceType == typeof(ResumeDbContext) ||
                    (d.ServiceType.IsGenericType && d.ServiceType.GetGenericArguments().Contains(typeof(ResumeDbContext))))
                .ToList();

            foreach (var descriptor in pooledDescriptors)
            {
                services.Remove(descriptor);
            }

            // AddDbContext registers DbContextOptions<T> as scoped by default,
            // so this configure callback re-runs for every new DI scope (i.e.
            // every request). Generating the database name outside the lambda
            // ensures every scope points at the same underlying InMemory
            // database instead of a fresh, empty one each time.
            var databaseName = Guid.NewGuid().ToString();
            services.AddDbContext<ResumeDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // The InMemory provider only applies OnModelCreating's HasData seed
        // once the database is created; unlike a real Postgres database
        // (created via migrations), nothing does that implicitly here.
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ResumeDbContext>();
        db.Database.EnsureCreated();

        return host;
    }
}
