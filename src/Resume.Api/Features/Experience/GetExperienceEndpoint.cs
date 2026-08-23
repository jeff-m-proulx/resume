using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Resume.Contracts;
using Resume.Data;

namespace Resume.Api.Features.Experience;

public class GetExperienceEndpoint(ResumeDbContext db) : EndpointWithoutRequest<List<ExperienceResponse>>
{
    public override void Configure()
    {
        Get("/api/experience");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var experience = await db.Experience
            .OrderByDescending(e => e.StartDate)
            .Select(e => new ExperienceResponse(e.Id, e.Company, e.JobTitle, e.Location, e.StartDate, e.EndDate, e.Highlights))
            .ToListAsync(ct);

        await Send.OkAsync(experience, cancellation: ct);
    }
}
