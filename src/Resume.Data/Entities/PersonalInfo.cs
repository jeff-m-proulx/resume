namespace Resume.Data.Entities;

public class PersonalInfo
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public required string Headline { get; set; }
    public string? Location { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public required string Summary { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? GitHubUrl { get; set; }
    public string? WebsiteUrl { get; set; }
}
