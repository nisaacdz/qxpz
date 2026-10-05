using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using CampFix.Data;
using CampFix.DTOs;
using CampFix.Hubs;
using CampFix.Models;

namespace CampFix.Endpoints;

public static class IssueEndpoints
{
    public static void MapIssueEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/issues").WithTags("Campus Issues");

        // GET /api/issues
        group.MapGet("/", async (
            AppDbContext db,
            IssueStatus? status,
            FacilityCategory? category,
            string? building,
            string? search) =>
        {
            var query = db.Issues
                .Include(i => i.Activities.OrderByDescending(a => a.CreatedAt))
                .AsNoTracking()
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(i => i.Status == status.Value);
            }

            if (category.HasValue)
            {
                query = query.Where(i => i.Category == category.Value);
            }

            if (!string.IsNullOrWhiteSpace(building))
            {
                query = query.Where(i => i.Building.ToLower() == building.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(i =>
                    i.Title.ToLower().Contains(term) ||
                    i.Description.ToLower().Contains(term) ||
                    i.Building.ToLower().Contains(term) ||
                    i.Room.ToLower().Contains(term));
            }

            var issues = await query
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return Results.Ok(issues);
        })
        .WithName("GetIssues")
        .WithSummary("List and filter campus facility issues");

        // GET /api/issues/stats
        group.MapGet("/stats", async (AppDbContext db) =>
        {
            var total = await db.Issues.CountAsync();
            var reported = await db.Issues.CountAsync(i => i.Status == IssueStatus.Reported);
            var investigating = await db.Issues.CountAsync(i => i.Status == IssueStatus.Investigating);
            var inProgress = await db.Issues.CountAsync(i => i.Status == IssueStatus.InProgress);
            var resolved = await db.Issues.CountAsync(i => i.Status == IssueStatus.Resolved);
            var closed = await db.Issues.CountAsync(i => i.Status == IssueStatus.Closed);
            var critical = await db.Issues.CountAsync(i => i.Priority == PriorityLevel.Critical && i.Status != IssueStatus.Resolved && i.Status != IssueStatus.Closed);

            return Results.Ok(new IssueStatsDto(
                total,
                reported,
                investigating,
                inProgress,
                resolved,
                closed,
                critical
            ));
        })
        .WithName("GetIssueStats")
        .WithSummary("Get statistical summary of facility tickets");

        // GET /api/issues/{id}
        group.MapGet("/{id:int}", async (int id, AppDbContext db) =>
        {
            var issue = await db.Issues
                .Include(i => i.Activities.OrderByDescending(a => a.CreatedAt))
                .FirstOrDefaultAsync(i => i.Id == id);

            return issue is not null ? Results.Ok(issue) : Results.NotFound(new { message = $"Issue with ID {id} not found" });
        })
        .WithName("GetIssueById")
        .WithSummary("Get details for a single issue");

        // POST /api/issues
        group.MapPost("/", async (
            CreateIssueDto dto,
            AppDbContext db,
            IHubContext<IssueHub> hub) =>
        {
            var issue = new Issue
            {
                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                Building = dto.Building.Trim(),
                Room = dto.Room.Trim(),
                Category = dto.Category,
                Priority = dto.Priority,
                Status = IssueStatus.Reported,
                ReporterName = dto.ReporterName.Trim(),
                ReporterEmail = dto.ReporterEmail.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Activities = new List<IssueActivity>
                {
                    new()
                    {
                        Author = dto.ReporterName.Trim(),
                        Action = "Reported",
                        Details = $"Issue logged under {dto.Category} with {dto.Priority} priority.",
                        CreatedAt = DateTime.UtcNow
                    }
                }
            };

            db.Issues.Add(issue);
            await db.SaveChangesAsync();

            // Broadcast real-time event to all connected clients
            await hub.Clients.All.SendAsync("IssueCreated", issue);

            return Results.Created($"/api/issues/{issue.Id}", issue);
        })
        .WithName("CreateIssue")
        .WithSummary("Submit a new campus facility issue report");

        // PATCH /api/issues/{id}/status
        group.MapPatch("/{id:int}/status", async (
            int id,
            UpdateStatusDto dto,
            AppDbContext db,
            IHubContext<IssueHub> hub) =>
        {
            var issue = await db.Issues
                .Include(i => i.Activities)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (issue is null)
            {
                return Results.NotFound(new { message = $"Issue #{id} not found" });
            }

            var previousStatus = issue.Status;
            issue.Status = dto.Status;
            issue.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(dto.AssignedTo))
            {
                issue.AssignedTo = dto.AssignedTo.Trim();
            }

            if (!string.IsNullOrWhiteSpace(dto.Notes))
            {
                issue.ResolutionNotes = dto.Notes.Trim();
            }

            var author = string.IsNullOrWhiteSpace(dto.ChangedBy) ? "Staff Triage" : dto.ChangedBy.Trim();
            var detailMsg = $"Status transitioned from {previousStatus} to {dto.Status}.";
            if (!string.IsNullOrWhiteSpace(dto.Notes))
            {
                detailMsg += $" Note: {dto.Notes.Trim()}";
            }

            issue.Activities.Add(new IssueActivity
            {
                Author = author,
                Action = "StatusChanged",
                Details = detailMsg,
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();

            // Broadcast real-time update
            await hub.Clients.All.SendAsync("IssueStatusUpdated", issue);

            return Results.Ok(issue);
        })
        .WithName("UpdateIssueStatus")
        .WithSummary("Update issue status, assigned personnel, or resolution notes");

        // POST /api/issues/{id}/comments
        group.MapPost("/{id:int}/comments", async (
            int id,
            AddCommentDto dto,
            AppDbContext db,
            IHubContext<IssueHub> hub) =>
        {
            var issue = await db.Issues
                .Include(i => i.Activities)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (issue is null)
            {
                return Results.NotFound(new { message = $"Issue #{id} not found" });
            }

            var activity = new IssueActivity
            {
                IssueId = id,
                Author = dto.Author.Trim(),
                Action = "CommentAdded",
                Details = dto.Comment.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            issue.Activities.Add(activity);
            issue.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            // Broadcast comment update
            await hub.Clients.All.SendAsync("CommentAdded", id, activity);

            return Results.Ok(activity);
        })
        .WithName("AddIssueComment")
        .WithSummary("Add a comment or follow-up note to an issue");

        // DELETE /api/issues/{id}
        group.MapDelete("/{id:int}", async (
            int id,
            AppDbContext db,
            IHubContext<IssueHub> hub) =>
        {
            var issue = await db.Issues.FindAsync(id);
            if (issue is null)
            {
                return Results.NotFound(new { message = $"Issue #{id} not found" });
            }

            db.Issues.Remove(issue);
            await db.SaveChangesAsync();

            // Broadcast deletion
            await hub.Clients.All.SendAsync("IssueDeleted", id);

            return Results.NoContent();
        })
        .WithName("DeleteIssue")
        .WithSummary("Delete or cancel an issue ticket");

        // GET /api/issues/meta
        app.MapGet("/api/meta", () =>
        {
            var categories = Enum.GetNames<FacilityCategory>();
            var priorities = Enum.GetNames<PriorityLevel>();
            var statuses = Enum.GetNames<IssueStatus>();
            var campusBuildings = new[]
            {
                "Computer Science Dept",
                "J.Q.B",
                "Balme Library",
                "Chemistry Block",
                "Volta Hall",
                "N Block",
                "CCB (Central Cafeteria Building)",
                "UG Medical Centre",
                "Great Hall",
                "Commonwealth Hall"
            };

            return Results.Ok(new
            {
                Categories = categories,
                Priorities = priorities,
                Statuses = statuses,
                Buildings = campusBuildings
            });
        })
        .WithTags("Campus Issues")
        .WithName("GetMetadata")
        .WithSummary("Get available categories, buildings, and statuses");
    }
}
