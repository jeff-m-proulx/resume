using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Resume.Contracts;
using Resume.Data;

namespace Resume.Api.Features.PersonalInfo;

public class UpdatePersonalInfoValidator : Validator<UpdatePersonalInfoRequest>
{
    public UpdatePersonalInfoValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Headline).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Summary).NotEmpty().MaximumLength(2000);
    }
}

public class UpdatePersonalInfoEndpoint(ResumeDbContext db) : Endpoint<UpdatePersonalInfoRequest, PersonalInfoResponse>
{
    public override void Configure()
    {
        Put("/api/personal-info");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdatePersonalInfoRequest req, CancellationToken ct)
    {
        // Intentionally not persisted: this demo API validates and echoes the
        // submitted data back but never calls SaveChangesAsync.
        var existing = await db.PersonalInfo.SingleAsync(ct);

        await Send.OkAsync(new PersonalInfoResponse(
            existing.Id,
            req.FullName,
            req.Headline,
            req.Location,
            req.Email,
            req.Phone,
            req.Summary,
            req.LinkedInUrl,
            req.GitHubUrl,
            req.WebsiteUrl), cancellation: ct);
    }
}
