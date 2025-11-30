using FamilyWall.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FamilyWall.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<Media> Media { get; set; } = null!;
    public DbSet<MediaTag> MediaTags { get; set; } = null!;
    public DbSet<CalendarEvent> CalendarEvents { get; set; } = null!;
    public DbSet<CalendarConfiguration> CalendarConfigurations { get; set; } = null!;
    public DbSet<CalendarSubscription> CalendarSubscriptions { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Media>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Source);
            entity.HasIndex(e => e.TakenUtc);
            entity.HasIndex(e => e.Favorite);
            entity.HasIndex(e => e.LastShownUtc);
            entity.Property(e => e.Path).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Source).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Sha1).HasMaxLength(40);
        });

        modelBuilder.Entity<MediaTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.MediaId, e.Tag });
            entity.Property(e => e.Tag).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.Media)
                .WithMany(m => m.Tags)
                .HasForeignKey(e => e.MediaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CalendarEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProviderKey).IsUnique();
            entity.HasIndex(e => e.CalendarId);
            entity.HasIndex(e => new { e.StartUtc, e.EndUtc });
            entity.HasIndex(e => new { e.Source, e.CalendarId });
            entity.HasIndex(e => e.IsRecurring);
            entity.HasIndex(e => e.LastSyncUtc);
            entity.Property(e => e.ProviderKey).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CalendarId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Source).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.Calendar)
                .WithMany(c => c.Events)
                .HasForeignKey(e => e.CalendarId)
                .HasPrincipalKey(c => c.CalendarId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CalendarConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Source, e.CalendarId }).IsUnique();
            entity.HasIndex(e => e.IsEnabled);
            entity.HasIndex(e => e.DisplayOrder);
            entity.Property(e => e.CalendarId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Source).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Color).IsRequired().HasMaxLength(20);
        });

        modelBuilder.Entity<CalendarSubscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Url).IsUnique();
            entity.HasIndex(e => e.IsEnabled);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Source).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Color).IsRequired().HasMaxLength(20);
        });
    }
}
