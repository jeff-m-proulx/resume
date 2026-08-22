namespace Resume.Data.Entities;

public class Education
{
    public Guid Id { get; set; }
    public required string Institution { get; set; }
    public required string Degree { get; set; }
    public string? FieldOfStudy { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string[] Details { get; set; } = [];
}
