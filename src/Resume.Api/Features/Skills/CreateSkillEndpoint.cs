using FastEndpoints;
using FluentValidation;
using Resume.Contracts;
using Resume.Data;

namespace Resume.Api.Features.Skills;

public class CreateSkillValidator : Validator<CreateSkillRequest>
{
    public CreateSkillValidator()
    {
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public class CreateSkillEndpoint(ResumeDbContext db) : Endpoint<CreateSkillRequest, SkillResponse>
{
    public override void Configure()
    {
        Post("/api/skills");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateSkillRequest req, CancellationToken ct)
    {
        // Intentionally not persisted: see UpdatePersonalInfoEndpoint.
        await Send.OkAsync(new SkillResponse(Guid.NewGuid(), req.Category, req.Name, req.SortOrder), cancellation: ct);
    }
}
