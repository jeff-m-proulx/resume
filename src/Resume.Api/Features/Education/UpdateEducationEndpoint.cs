using FastEndpoints;
using FluentValidation;
using Resume.Contracts;
using Resume.Data;

namespace Resume.Api.Features.Education;

public class UpdateEducationValidator : Validator<UpdateEducationRequest>
{
    public UpdateEducationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Institution).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Degree).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EndDate)
            .Must((req, endDate) => endDate is null || endDate >= req.StartDate)
            .WithMessage("EndDate must be on or after StartDate.");
    }
}

public class UpdateEducationEndpoint(ResumeDbContext db) : Endpoint<UpdateEducationRequest, EducationResponse>
{
    public override void Configure()
    {
        Put("/api/education/{Id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateEducationRequest req, CancellationToken ct)
    {
        // Intentionally not persisted: see UpdatePersonalInfoEndpoint.
        await Send.OkAsync(new EducationResponse(req.Id, req.Institution, req.Degree, req.FieldOfStudy, req.StartDate, req.EndDate, req.Details), cancellation: ct);
    }
}
