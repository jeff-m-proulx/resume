using System.Net;
using System.Net.Http.Json;
using Resume.Contracts;
using Resume.Data;
using Xunit;

namespace Resume.Api.Tests;

public class EducationEndpointTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    [Fact]
    public async Task Get_returns_all_three_seeded_education_entries()
    {
        var client = fixture.CreateClient();

        var education = await client.GetFromJsonAsync<List<EducationResponse>>("/api/education");

        Assert.NotNull(education);
        Assert.Equal(3, education.Count);
    }

    [Fact]
    public async Task Post_with_valid_data_returns_created_entry_without_persisting()
    {
        var client = fixture.CreateClient();
        var request = new CreateEducationRequest(
            Institution: "Tech Institute",
            Degree: "M.S.",
            FieldOfStudy: "Software Engineering",
            StartDate: new DateOnly(2019, 9, 1),
            EndDate: new DateOnly(2021, 5, 1),
            Details: ["Thesis on distributed systems."]);

        var response = await client.PostAsJsonAsync("/api/education", request);
        var created = await response.Content.ReadFromJsonAsync<EducationResponse>();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("Tech Institute", created!.Institution);

        var educationAfter = await client.GetFromJsonAsync<List<EducationResponse>>("/api/education");
        Assert.Equal(3, educationAfter!.Count);
    }

    [Fact]
    public async Task Put_with_valid_data_returns_updated_entry_without_persisting()
    {
        var client = fixture.CreateClient();
        var seeded = SeedData.Education[0];
        var request = new UpdateEducationRequest(
            seeded.Id, Institution: "Changed University", Degree: seeded.Degree, FieldOfStudy: seeded.FieldOfStudy,
            StartDate: seeded.StartDate, EndDate: seeded.EndDate, Details: seeded.Details);

        var response = await client.PutAsJsonAsync($"/api/education/{request.Id}", request);
        var updated = await response.Content.ReadFromJsonAsync<EducationResponse>();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("Changed University", updated!.Institution);

        var educationAfter = await client.GetFromJsonAsync<List<EducationResponse>>("/api/education");
        Assert.Contains(educationAfter!, e => e.Id == seeded.Id && e.Institution == seeded.Institution);
    }

    [Fact]
    public async Task Post_with_missing_institution_returns_validation_error()
    {
        var client = fixture.CreateClient();
        var request = new CreateEducationRequest(
            Institution: "",
            Degree: "M.S.",
            FieldOfStudy: null,
            StartDate: new DateOnly(2019, 9, 1),
            EndDate: null,
            Details: []);

        var response = await client.PostAsJsonAsync("/api/education", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_with_missing_institution_returns_validation_error()
    {
        var client = fixture.CreateClient();
        var seeded = SeedData.Education[0];
        var request = new UpdateEducationRequest(
            seeded.Id, Institution: "", Degree: seeded.Degree, FieldOfStudy: seeded.FieldOfStudy,
            StartDate: seeded.StartDate, EndDate: seeded.EndDate, Details: seeded.Details);

        var response = await client.PutAsJsonAsync($"/api/education/{request.Id}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
