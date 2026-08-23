using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Resume.Contracts;
using Resume.Data;

namespace Resume.Api.Features.Skills;

public class GetSkillsEndpoint(ResumeDbContext db) : EndpointWithoutRequest<List<SkillResponse>>
{
    public override void Configure()
    {
        Get("/api/skills");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var skills = await db.Skills
            .OrderBy(s => s.Category)
            .ThenBy(s => s.SortOrder)
            .Select(s => new SkillResponse(s.Id, s.Category, s.Name, s.SortOrder))
            .ToListAsync(ct);

        await Send.OkAsync(skills, cancellation: ct);
    }
}
