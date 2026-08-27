using System.Net;
using System.Net.Http.Json;
using Resume.Contracts;
using Xunit;

namespace Resume.Api.Tests;

public class SkillsEndpointTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    [Fact]
    public async Task Get_returns_no_seeded_skills()
    {
        var client = fixture.CreateClient();

        var skills = await client.GetFromJsonAsync<List<SkillResponse>>("/api/skills");

        Assert.NotNull(skills);
        Assert.Empty(skills);
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
        Assert.Empty(skillsAfter!);
    }

    [Fact]
    public async Task Put_with_valid_data_returns_updated_skill_without_persisting()
    {
        var client = fixture.CreateClient();
        var request = new UpdateSkillRequest(Guid.NewGuid(), Category: "Languages", Name: "C# (Updated)", SortOrder: 1);

        var response = await client.PutAsJsonAsync($"/api/skills/{request.Id}", request);
        var updated = await response.Content.ReadFromJsonAsync<SkillResponse>();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("C# (Updated)", updated!.Name);

        var skillsAfter = await client.GetFromJsonAsync<List<SkillResponse>>("/api/skills");
        Assert.Empty(skillsAfter!);
    }

    [Fact]
    public async Task Post_with_empty_name_returns_validation_error()
    {
        var client = fixture.CreateClient();
        var request = new CreateSkillRequest(Category: "Languages", Name: "", SortOrder: 1);

        var response = await client.PostAsJsonAsync("/api/skills", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_with_empty_name_returns_validation_error()
    {
        var client = fixture.CreateClient();
        var request = new UpdateSkillRequest(Guid.NewGuid(), Category: "Languages", Name: "", SortOrder: 1);

        var response = await client.PutAsJsonAsync($"/api/skills/{request.Id}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
