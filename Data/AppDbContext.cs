using Microsoft.EntityFrameworkCore;
using CampFix.Models;

namespace CampFix.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<IssueActivity> Activities => Set<IssueActivity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Issue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Building).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Room).IsRequired().HasMaxLength(50);
            
            // Store Enums as human-friendly strings in DB
            entity.Property(e => e.Category).HasConversion<string>();
            entity.Property(e => e.Priority).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Building);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasMany(e => e.Activities)
                  .WithOne(a => a.Issue)
                  .HasForeignKey(a => a.IssueId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IssueActivity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Author).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Details).IsRequired();
        });
    }
}
