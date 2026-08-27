using System.Net;
using System.Net.Http.Json;
using Resume.Contracts;
using Resume.Data;
using Xunit;

namespace Resume.Api.Tests;

public class ExperienceEndpointTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    [Fact]
    public async Task Get_returns_all_seven_seeded_experience_entries()
    {
        var client = fixture.CreateClient();

        var experience = await client.GetFromJsonAsync<List<ExperienceResponse>>("/api/experience");

        Assert.NotNull(experience);
        Assert.Equal(7, experience.Count);
    }

    [Fact]
    public async Task Post_with_valid_data_returns_created_entry_without_persisting()
    {
        var client = fixture.CreateClient();
        var request = new CreateExperienceRequest(
            Company: "Acme Corp",
            JobTitle: "Staff Engineer",
            Location: "Austin, TX",
            StartDate: new DateOnly(2024, 1, 1),
            EndDate: null,
            Highlights: ["Shipped a new feature."]);

        var response = await client.PostAsJsonAsync("/api/experience", request);
        var created = await response.Content.ReadFromJsonAsync<ExperienceResponse>();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("Acme Corp", created!.Company);

        var experienceAfter = await client.GetFromJsonAsync<List<ExperienceResponse>>("/api/experience");
        Assert.Equal(7, experienceAfter!.Count);
    }

    [Fact]
    public async Task Put_with_valid_data_returns_updated_entry_without_persisting()
    {
        var client = fixture.CreateClient();
        var seeded = SeedData.Experience[0];
        var request = new UpdateExperienceRequest(
            seeded.Id, Company: "Changed Co", JobTitle: seeded.JobTitle, Location: seeded.Location,
            StartDate: seeded.StartDate, EndDate: seeded.EndDate, Highlights: seeded.Highlights);

        var response = await client.PutAsJsonAsync($"/api/experience/{request.Id}", request);
        var updated = await response.Content.ReadFromJsonAsync<ExperienceResponse>();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("Changed Co", updated!.Company);

        var experienceAfter = await client.GetFromJsonAsync<List<ExperienceResponse>>("/api/experience");
        Assert.Contains(experienceAfter!, e => e.Id == seeded.Id && e.Company == seeded.Company);
    }

    [Fact]
    public async Task Post_with_end_date_before_start_date_returns_validation_error()
    {
        var client = fixture.CreateClient();
        var request = new CreateExperienceRequest(
            Company: "Acme Corp",
            JobTitle: "Staff Engineer",
            Location: null,
            StartDate: new DateOnly(2024, 1, 1),
            EndDate: new DateOnly(2023, 1, 1),
            Highlights: ["Something."]);

        var response = await client.PostAsJsonAsync("/api/experience", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_with_end_date_before_start_date_returns_validation_error()
    {
        var client = fixture.CreateClient();
        var seeded = SeedData.Experience[0];
        var request = new UpdateExperienceRequest(
            seeded.Id, Company: seeded.Company, JobTitle: seeded.JobTitle, Location: seeded.Location,
            StartDate: new DateOnly(2024, 1, 1), EndDate: new DateOnly(2023, 1, 1), Highlights: seeded.Highlights);

        var response = await client.PutAsJsonAsync($"/api/experience/{request.Id}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
