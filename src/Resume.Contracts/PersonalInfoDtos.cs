namespace Resume.Contracts;

public record PersonalInfoResponse(
    Guid Id,
    string FullName,
    string Headline,
    string? Location,
    string Email,
    string? Phone,
    string Summary,
    string? LinkedInUrl,
    string? GitHubUrl,
    string? WebsiteUrl);

public record UpdatePersonalInfoRequest(
    string FullName,
    string Headline,
    string? Location,
    string Email,
    string? Phone,
    string Summary,
    string? LinkedInUrl,
    string? GitHubUrl,
    string? WebsiteUrl);
