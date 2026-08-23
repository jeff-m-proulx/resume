namespace Resume.Contracts;

public record SkillResponse(Guid Id, string Category, string Name, int SortOrder);

public record CreateSkillRequest(string Category, string Name, int SortOrder);

public record UpdateSkillRequest(Guid Id, string Category, string Name, int SortOrder);
