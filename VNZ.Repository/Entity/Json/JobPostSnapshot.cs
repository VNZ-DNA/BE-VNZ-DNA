using VNZ.Repository.Entity.Enum;

namespace VNZ.Repository.Entity.Json;

public class JobPostSnapshot
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public EmploymentType EmploymentType { get; set; }
    public JobLevel JobLevel { get; set; }
    public int NumberOfPositions { get; set; }
    public string ShortDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Requirements { get; set; } = string.Empty;
    public DateTimeOffset? ExpiredAt { get; set; }
    public JobSkillsSnapshot JobSkillsSnapshot { get; set; } = new();
}

public class JobSkillsSnapshot
{
    public List<string> Skills { get; set; } = new();
}
