using FastEndpoints;
using FluentValidation;
using Resume.Contracts;
using Resume.Data;

namespace Resume.Api.Features.Skills;

public class UpdateSkillValidator : Validator<UpdateSkillRequest>
{
    public UpdateSkillValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public class UpdateSkillEndpoint(ResumeDbContext db) : Endpoint<UpdateSkillRequest, SkillResponse>
{
    public override void Configure()
    {
        Put("/api/skills/{Id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateSkillRequest req, CancellationToken ct)
    {
        // Intentionally not persisted: see UpdatePersonalInfoEndpoint.
        await Send.OkAsync(new SkillResponse(req.Id, req.Category, req.Name, req.SortOrder), cancellation: ct);
    }
}
