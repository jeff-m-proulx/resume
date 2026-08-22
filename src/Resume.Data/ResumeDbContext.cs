using Microsoft.EntityFrameworkCore;
using Resume.Data.Entities;

namespace Resume.Data;

public class ResumeDbContext(DbContextOptions<ResumeDbContext> options) : DbContext(options)
{
    public DbSet<PersonalInfo> PersonalInfo => Set<PersonalInfo>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<Experience> Experience => Set<Experience>();
    public DbSet<Education> Education => Set<Education>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PersonalInfo>().HasData(SeedData.PersonalInfo);
        modelBuilder.Entity<Skill>().HasData(SeedData.Skills);
        modelBuilder.Entity<Experience>().HasData(SeedData.Experience);
        modelBuilder.Entity<Education>().HasData(SeedData.Education);
    }
}
