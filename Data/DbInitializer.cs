using CampFix.Models;

namespace CampFix.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (db.Issues.Any())
        {
            return; // DB has already been seeded
        }

        var sampleIssues = new List<Issue>
        {
            new()
            {
                Title = "Ceiling water leak near server rack",
                Description = "Water is slowly dripping through ceiling tiles directly adjacent to the departmental networking rack. Urgent attention needed to prevent water damage to switches.",
                Building = "Computer Science Dept",
                Room = "Server Room 102",
                Category = FacilityCategory.Plumbing,
                Priority = PriorityLevel.Critical,
                Status = IssueStatus.InProgress,
                ReporterName = "Dr. Michael Soli",
                ReporterEmail = "msoli@ug.edu.gh",
                AssignedTo = "Campus Works Directorate",
                ResolutionNotes = "Maintenance crew deployed with tarps and leak detection equipment.",
                CreatedAt = DateTime.UtcNow.AddHours(-18),
                UpdatedAt = DateTime.UtcNow.AddHours(-2),
                Activities = new List<IssueActivity>
                {
                    new()
                    {
                        Author = "Dr. Michael Soli",
                        Action = "Reported",
                        Details = "Ticket created with Critical priority.",
                        CreatedAt = DateTime.UtcNow.AddHours(-18)
                    },
                    new()
                    {
                        Author = "Admin Triage",
                        Action = "Assigned",
                        Details = "Assigned to Campus Works Directorate (Lead: Ing. Boateng).",
                        CreatedAt = DateTime.UtcNow.AddHours(-16)
                    },
                    new()
                    {
                        Author = "Ing. Boateng",
                        Action = "StatusChanged",
                        Details = "Status updated to In Progress. Isolation valve temporarily closed.",
                        CreatedAt = DateTime.UtcNow.AddHours(-2)
                    }
                }
            },
            new()
            {
                Title = "AC compressor grinding loudly and blowing warm air",
                Description = "The split-unit air conditioner in JQB 12 is vibrating intensely and not cooling. Mid-day lectures are unbearable.",
                Building = "J.Q.B",
                Room = "Lecture Hall 12",
                Category = FacilityCategory.HVAC,
                Priority = PriorityLevel.High,
                Status = IssueStatus.Investigating,
                ReporterName = "Kofi Mensah",
                ReporterEmail = "kmensah102@st.ug.edu.gh",
                AssignedTo = "HVAC Support Team",
                CreatedAt = DateTime.UtcNow.AddHours(-10),
                UpdatedAt = DateTime.UtcNow.AddHours(-4),
                Activities = new List<IssueActivity>
                {
                    new()
                    {
                        Author = "Kofi Mensah",
                        Action = "Reported",
                        Details = "Reported by class representative.",
                        CreatedAt = DateTime.UtcNow.AddHours(-10)
                    },
                    new()
                    {
                        Author = "System",
                        Action = "StatusChanged",
                        Details = "Triage team moved status to Investigating.",
                        CreatedAt = DateTime.UtcNow.AddHours(-4)
                    }
                }
            },
            new()
            {
                Title = "Flickering HDMI output on ceiling projector",
                Description = "The overhead projector periodically loses HDMI handshake every 2 minutes when laptops connect via the podium wall plate.",
                Building = "Balme Library",
                Room = "Seminar Room B",
                Category = FacilityCategory.IT_AV,
                Priority = PriorityLevel.Medium,
                Status = IssueStatus.Reported,
                ReporterName = "Ama Serwaa",
                ReporterEmail = "aserwaa@ug.edu.gh",
                CreatedAt = DateTime.UtcNow.AddHours(-5),
                UpdatedAt = DateTime.UtcNow.AddHours(-5),
                Activities = new List<IssueActivity>
                {
                    new()
                    {
                        Author = "Ama Serwaa",
                        Action = "Reported",
                        Details = "Issue logged following morning research presentation.",
                        CreatedAt = DateTime.UtcNow.AddHours(-5)
                    }
                }
            },
            new()
            {
                Title = "Broken hydraulic door closer on main lab entry",
                Description = "The door closer arm snapped, causing the fire door to slam aggressively on anyone entering or leaving the lab.",
                Building = "Chemistry Block",
                Room = "Lab 4 Entrance",
                Category = FacilityCategory.Structural,
                Priority = PriorityLevel.Medium,
                Status = IssueStatus.Resolved,
                ReporterName = "Lab Tech E. Darko",
                ReporterEmail = "edarko@ug.edu.gh",
                AssignedTo = "Carpentry & Hardware Shop",
                ResolutionNotes = "Replaced closer arm with heavy-duty Dorma unit. Tested latch speed.",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddHours(-8),
                Activities = new List<IssueActivity>
                {
                    new()
                    {
                        Author = "Lab Tech E. Darko",
                        Action = "Reported",
                        Details = "Reported safety hazard.",
                        CreatedAt = DateTime.UtcNow.AddDays(-2)
                    },
                    new()
                    {
                        Author = "Carpentry Team",
                        Action = "Resolved",
                        Details = "Repaired and calibrated hydraulic damper.",
                        CreatedAt = DateTime.UtcNow.AddHours(-8)
                    }
                }
            },
            new()
            {
                Title = "Water supply intermittent on 2nd floor restrooms",
                Description = "Low pressure prevents flush valves from resetting properly, resulting in continuous running water or empty tanks.",
                Building = "Volta Hall",
                Room = "Block C - 2nd Floor",
                Category = FacilityCategory.Plumbing,
                Priority = PriorityLevel.High,
                Status = IssueStatus.Reported,
                ReporterName = "Akosua Addo",
                ReporterEmail = "aaddo@st.ug.edu.gh",
                CreatedAt = DateTime.UtcNow.AddHours(-3),
                UpdatedAt = DateTime.UtcNow.AddHours(-3),
                Activities = new List<IssueActivity>
                {
                    new()
                    {
                        Author = "Akosua Addo",
                        Action = "Reported",
                        Details = "Logged via student portal.",
                        CreatedAt = DateTime.UtcNow.AddHours(-3)
                    }
                }
            }
        };

        await db.Issues.AddRangeAsync(sampleIssues);
        await db.SaveChangesAsync();
    }
}
