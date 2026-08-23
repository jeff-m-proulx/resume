using FastEndpoints;
using FluentValidation;
using Resume.Contracts;
using Resume.Data;

namespace Resume.Api.Features.Education;

public class CreateEducationValidator : Validator<CreateEducationRequest>
{
    public CreateEducationValidator()
    {
        RuleFor(x => x.Institution).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Degree).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EndDate)
            .Must((req, endDate) => endDate is null || endDate >= req.StartDate)
            .WithMessage("EndDate must be on or after StartDate.");
    }
}

public class CreateEducationEndpoint(ResumeDbContext db) : Endpoint<CreateEducationRequest, EducationResponse>
{
    public override void Configure()
    {
        Post("/api/education");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateEducationRequest req, CancellationToken ct)
    {
        // Intentionally not persisted: see UpdatePersonalInfoEndpoint.
        await Send.OkAsync(new EducationResponse(Guid.NewGuid(), req.Institution, req.Degree, req.FieldOfStudy, req.StartDate, req.EndDate, req.Details), cancellation: ct);
    }
}
