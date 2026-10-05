using System.ComponentModel.DataAnnotations;
using CampFix.Models;

namespace CampFix.DTOs;

public record CreateIssueDto(
    [Required, MinLength(5), MaxLength(120)] string Title,
    [Required, MinLength(10)] string Description,
    [Required] string Building,
    [Required] string Room,
    FacilityCategory Category,
    PriorityLevel Priority,
    [Required] string ReporterName,
    [EmailAddress] string ReporterEmail
);

public record UpdateStatusDto(
    [Required] IssueStatus Status,
    string? ChangedBy,
    string? Notes,
    string? AssignedTo
);

public record AddCommentDto(
    [Required] string Author,
    [Required, MinLength(2)] string Comment
);

public record IssueStatsDto(
    int TotalIssues,
    int ReportedCount,
    int InvestigatingCount,
    int InProgressCount,
    int ResolvedCount,
    int ClosedCount,
    int CriticalCount
);
