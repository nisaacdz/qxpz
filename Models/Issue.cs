using System.Text.Json.Serialization;

namespace CampFix.Models;

public class Issue
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public string Building { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    
    public FacilityCategory Category { get; set; } = FacilityCategory.Other;
    public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;
    public IssueStatus Status { get; set; } = IssueStatus.Reported;
    
    public string ReporterName { get; set; } = string.Empty;
    public string ReporterEmail { get; set; } = string.Empty;
    
    public string? AssignedTo { get; set; }
    public string? ResolutionNotes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<IssueActivity> Activities { get; set; } = new();
}

public class IssueActivity
{
    public int Id { get; set; }
    public int IssueId { get; set; }
    
    [JsonIgnore]
    public Issue? Issue { get; set; }
    
    public string Author { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
