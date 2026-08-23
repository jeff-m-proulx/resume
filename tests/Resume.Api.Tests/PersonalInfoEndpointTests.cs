using System.Net;
using System.Net.Http.Json;
using Resume.Contracts;
using Resume.Data;
using Xunit;

namespace Resume.Api.Tests;

public class PersonalInfoEndpointTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    [Fact]
    public async Task Get_returns_seeded_personal_info()
    {
        var client = fixture.CreateClient();

        var response = await client.GetFromJsonAsync<PersonalInfoResponse>("/api/personal-info");

        Assert.NotNull(response);
        Assert.Equal(SeedData.PersonalInfoId, response.Id);
        Assert.Equal("Jordan Rivera", response.FullName);
    }

    [Fact]
    public async Task Put_with_valid_data_returns_submitted_values_without_persisting()
    {
        var client = fixture.CreateClient();
        var request = new UpdatePersonalInfoRequest(
            FullName: "Changed Name",
            Headline: "Changed Headline",
            Email: "changed@example.com",
            Phone: "555-9999",
            Summary: "Changed summary.",
            LinkedInUrl: null,
            GitHubUrl: null,
            WebsiteUrl: null);

        var putResponse = await client.PutAsJsonAsync("/api/personal-info", request);
        var updated = await putResponse.Content.ReadFromJsonAsync<PersonalInfoResponse>();

        Assert.True(putResponse.IsSuccessStatusCode);
        Assert.Equal("Changed Name", updated!.FullName);

        var getAfter = await client.GetFromJsonAsync<PersonalInfoResponse>("/api/personal-info");
        Assert.Equal("Jordan Rivera", getAfter!.FullName);
    }

    [Fact]
    public async Task Put_with_missing_full_name_returns_validation_error()
    {
        var client = fixture.CreateClient();
        var request = new UpdatePersonalInfoRequest(
            FullName: "",
            Headline: "Headline",
            Email: "valid@example.com",
            Phone: null,
            Summary: "Summary.",
            LinkedInUrl: null,
            GitHubUrl: null,
            WebsiteUrl: null);

        var response = await client.PutAsJsonAsync("/api/personal-info", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
