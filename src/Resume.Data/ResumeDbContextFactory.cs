using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Resume.Data;

public class ResumeDbContextFactory : IDesignTimeDbContextFactory<ResumeDbContext>
{
    public ResumeDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ResumeDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=resumedb;Username=postgres;Password=postgres");
        return new ResumeDbContext(optionsBuilder.Options);
    }
}
