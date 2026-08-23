namespace Resume.Contracts;

public record ExperienceResponse(
    Guid Id,
    string Company,
    string JobTitle,
    string? Location,
    DateOnly StartDate,
    DateOnly? EndDate,
    string[] Highlights);

public record CreateExperienceRequest(
    string Company,
    string JobTitle,
    string? Location,
    DateOnly StartDate,
    DateOnly? EndDate,
    string[] Highlights);

public record UpdateExperienceRequest(
    Guid Id,
    string Company,
    string JobTitle,
    string? Location,
    DateOnly StartDate,
    DateOnly? EndDate,
    string[] Highlights);
