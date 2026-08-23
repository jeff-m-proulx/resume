using FastEndpoints;
using FluentValidation;
using Resume.Contracts;
using Resume.Data;

namespace Resume.Api.Features.Experience;

public class UpdateExperienceValidator : Validator<UpdateExperienceRequest>
{
    public UpdateExperienceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Company).NotEmpty().MaximumLength(200);
        RuleFor(x => x.JobTitle).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Highlights).Must(h => h.Length > 0).WithMessage("At least one highlight is required.");
        RuleFor(x => x.EndDate)
            .Must((req, endDate) => endDate is null || endDate >= req.StartDate)
            .WithMessage("EndDate must be on or after StartDate.");
    }
}

public class UpdateExperienceEndpoint(ResumeDbContext db) : Endpoint<UpdateExperienceRequest, ExperienceResponse>
{
    public override void Configure()
    {
        Put("/api/experience/{Id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateExperienceRequest req, CancellationToken ct)
    {
        // Intentionally not persisted: see UpdatePersonalInfoEndpoint.
        await Send.OkAsync(new ExperienceResponse(req.Id, req.Company, req.JobTitle, req.Location, req.StartDate, req.EndDate, req.Highlights), cancellation: ct);
    }
}
