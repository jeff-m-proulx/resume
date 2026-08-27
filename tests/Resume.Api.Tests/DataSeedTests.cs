using Microsoft.EntityFrameworkCore;
using Resume.Data;
using Xunit;

namespace Resume.Api.Tests;

public class DataSeedTests
{
    private static ResumeDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ResumeDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var context = new ResumeDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public void Seeded_database_contains_one_personal_info_record()
    {
        using var context = CreateInMemoryContext(nameof(Seeded_database_contains_one_personal_info_record));

        var personalInfo = Assert.Single(context.PersonalInfo);
        Assert.Equal(SeedData.PersonalInfoId, personalInfo.Id);
        Assert.Equal("Jeffrey M. Proulx", personalInfo.FullName);
    }

    [Fact]
    public void Seeded_database_contains_no_skills()
    {
        using var context = CreateInMemoryContext(nameof(Seeded_database_contains_no_skills));

        Assert.Empty(context.Skills);
    }

    [Fact]
    public void Seeded_database_contains_seven_experience_entries()
    {
        using var context = CreateInMemoryContext(nameof(Seeded_database_contains_seven_experience_entries));

        var experience = context.Experience.ToList();
        Assert.Equal(7, experience.Count);
    }

    [Fact]
    public void Seeded_database_contains_three_education_entries()
    {
        using var context = CreateInMemoryContext(nameof(Seeded_database_contains_three_education_entries));

        var education = context.Education.ToList();
        Assert.Equal(3, education.Count);
        Assert.Contains(education, e => e.Institution == "DeVry University");
    }
}
