namespace Resume.Data.Entities;

public class Experience
{
    public Guid Id { get; set; }
    public required string Company { get; set; }
    public required string JobTitle { get; set; }
    public string? Location { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string[] Highlights { get; set; } = [];
}
