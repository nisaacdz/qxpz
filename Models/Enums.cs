namespace CampFix.Models;

public enum IssueStatus
{
    Reported = 0,
    Investigating = 1,
    InProgress = 2,
    Resolved = 3,
    Closed = 4
}

public enum PriorityLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum FacilityCategory
{
    Electrical = 0,
    HVAC = 1,
    IT_AV = 2,
    Plumbing = 3,
    Structural = 4,
    Sanitation = 5,
    Other = 6
}
