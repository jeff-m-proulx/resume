namespace Resume.Contracts;

public record EducationResponse(
    Guid Id,
    string Institution,
    string Degree,
    string? FieldOfStudy,
    DateOnly StartDate,
    DateOnly? EndDate,
    string[] Details);

public record CreateEducationRequest(
    string Institution,
    string Degree,
    string? FieldOfStudy,
    DateOnly StartDate,
    DateOnly? EndDate,
    string[] Details);

public record UpdateEducationRequest(
    Guid Id,
    string Institution,
    string Degree,
    string? FieldOfStudy,
    DateOnly StartDate,
    DateOnly? EndDate,
    string[] Details);
