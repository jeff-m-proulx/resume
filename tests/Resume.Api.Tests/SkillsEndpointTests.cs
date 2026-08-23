using System.Net;
using System.Net.Http.Json;
using Resume.Contracts;
using Resume.Data;
using Xunit;

namespace Resume.Api.Tests;

public class SkillsEndpointTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    [Fact]
    public async Task Get_returns_all_ten_seeded_skills()
    {
        var client = fixture.CreateClient();

        var skills = await client.GetFromJsonAsync<List<SkillResponse>>("/api/skills");

        Assert.NotNull(skills);
        Assert.Equal(10, skills.Count);
    }

    [Fact]
    public async Task Post_with_valid_data_returns_created_skill_without_persisting()
    {
        var client = fixture.CreateClient();
        var request = new CreateSkillRequest(Category: "Languages", Name: "Rust", SortOrder: 4);

        var response = await client.PostAsJsonAsync("/api/skills", request);
        var created = await response.Content.ReadFromJsonAsync<SkillResponse>();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("Rust", created!.Name);
        Assert.NotEqual(Guid.Empty, created.Id);

        var skillsAfter = await client.GetFromJsonAsync<List<SkillResponse>>("/api/skills");
        Assert.Equal(10, skillsAfter!.Count);
    }

    [Fact]
    public async Task Put_with_valid_data_returns_updated_skill_without_persisting()
    {
        var client = fixture.CreateClient();
        var request = new UpdateSkillRequest(SeedData.Skills[0].Id, Category: "Languages", Name: "C# (Updated)", SortOrder: 1);

        var response = await client.PutAsJsonAsync($"/api/skills/{request.Id}", request);
        var updated = await response.Content.ReadFromJsonAsync<SkillResponse>();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("C# (Updated)", updated!.Name);

        var skillsAfter = await client.GetFromJsonAsync<List<SkillResponse>>("/api/skills");
        Assert.Contains(skillsAfter!, s => s.Id == SeedData.Skills[0].Id && s.Name == "C#");
    }

    [Fact]
    public async Task Post_with_empty_name_returns_validation_error()
    {
        var client = fixture.CreateClient();
        var request = new CreateSkillRequest(Category: "Languages", Name: "", SortOrder: 1);

        var response = await client.PostAsJsonAsync("/api/skills", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
