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
        Assert.Equal("Jordan Rivera", personalInfo.FullName);
    }

    [Fact]
    public void Seeded_database_contains_ten_skills_across_four_categories()
    {
        using var context = CreateInMemoryContext(nameof(Seeded_database_contains_ten_skills_across_four_categories));

        var skills = context.Skills.ToList();
        Assert.Equal(10, skills.Count);
        Assert.Equal(4, skills.Select(s => s.Category).Distinct().Count());
    }

    [Fact]
    public void Seeded_database_contains_two_experience_entries_with_highlights()
    {
        using var context = CreateInMemoryContext(nameof(Seeded_database_contains_two_experience_entries_with_highlights));

        var experience = context.Experience.ToList();
        Assert.Equal(2, experience.Count);
        Assert.All(experience, e => Assert.NotEmpty(e.Highlights));
    }

    [Fact]
    public void Seeded_database_contains_one_education_entry()
    {
        using var context = CreateInMemoryContext(nameof(Seeded_database_contains_one_education_entry));

        var education = Assert.Single(context.Education);
        Assert.Equal("State University", education.Institution);
    }
}
