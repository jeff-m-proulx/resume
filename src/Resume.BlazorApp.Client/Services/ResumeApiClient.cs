using System.Net.Http.Json;
using Resume.Contracts;

namespace Resume.BlazorApp.Client.Services;

public class ResumeApiClient(HttpClient http)
{
    public async Task<PersonalInfoResponse> GetPersonalInfoAsync() =>
        await http.GetFromJsonAsync<PersonalInfoResponse>("personal-info")
        ?? throw new InvalidOperationException("Personal info response was empty.");

    public async Task<PersonalInfoResponse> UpdatePersonalInfoAsync(UpdatePersonalInfoRequest request)
    {
        var response = await http.PutAsJsonAsync("personal-info", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PersonalInfoResponse>())!;
    }

    public async Task<List<SkillResponse>> GetSkillsAsync() =>
        await http.GetFromJsonAsync<List<SkillResponse>>("skills") ?? [];

    public async Task<SkillResponse> CreateSkillAsync(CreateSkillRequest request)
    {
        var response = await http.PostAsJsonAsync("skills", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SkillResponse>())!;
    }

    public async Task<SkillResponse> UpdateSkillAsync(UpdateSkillRequest request)
    {
        var response = await http.PutAsJsonAsync($"skills/{request.Id}", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SkillResponse>())!;
    }

    public async Task<List<ExperienceResponse>> GetExperienceAsync() =>
        await http.GetFromJsonAsync<List<ExperienceResponse>>("experience") ?? [];

    public async Task<ExperienceResponse> CreateExperienceAsync(CreateExperienceRequest request)
    {
        var response = await http.PostAsJsonAsync("experience", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ExperienceResponse>())!;
    }

    public async Task<ExperienceResponse> UpdateExperienceAsync(UpdateExperienceRequest request)
    {
        var response = await http.PutAsJsonAsync($"experience/{request.Id}", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ExperienceResponse>())!;
    }

    public async Task<List<EducationResponse>> GetEducationAsync() =>
        await http.GetFromJsonAsync<List<EducationResponse>>("education") ?? [];

    public async Task<EducationResponse> CreateEducationAsync(CreateEducationRequest request)
    {
        var response = await http.PostAsJsonAsync("education", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EducationResponse>())!;
    }

    public async Task<EducationResponse> UpdateEducationAsync(UpdateEducationRequest request)
    {
        var response = await http.PutAsJsonAsync($"education/{request.Id}", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EducationResponse>())!;
    }
}
