using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Resume.Contracts;
using Resume.Data;

namespace Resume.Api.Features.Education;

public class GetEducationEndpoint(ResumeDbContext db) : EndpointWithoutRequest<List<EducationResponse>>
{
    public override void Configure()
    {
        Get("/api/education");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var education = await db.Education
            .OrderByDescending(e => e.StartDate)
            .Select(e => new EducationResponse(e.Id, e.Institution, e.Degree, e.FieldOfStudy, e.StartDate, e.EndDate, e.Details))
            .ToListAsync(ct);

        await Send.OkAsync(education, cancellation: ct);
    }
}
