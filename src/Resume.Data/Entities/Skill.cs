namespace Resume.Data.Entities;

public class Skill
{
    public Guid Id { get; set; }
    public required string Category { get; set; }
    public required string Name { get; set; }
    public int SortOrder { get; set; }
}
