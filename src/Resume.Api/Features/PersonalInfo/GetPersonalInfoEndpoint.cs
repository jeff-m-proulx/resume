using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Resume.Contracts;
using Resume.Data;

namespace Resume.Api.Features.PersonalInfo;

public class GetPersonalInfoEndpoint(ResumeDbContext db) : EndpointWithoutRequest<PersonalInfoResponse>
{
    public override void Configure()
    {
        Get("/api/personal-info");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var entity = await db.PersonalInfo.SingleAsync(ct);

        await Send.OkAsync(new PersonalInfoResponse(
            entity.Id,
            entity.FullName,
            entity.Headline,
            entity.Email,
            entity.Phone,
            entity.Summary,
            entity.LinkedInUrl,
            entity.GitHubUrl,
            entity.WebsiteUrl), cancellation: ct);
    }
}
