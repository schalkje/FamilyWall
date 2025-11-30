using FamilyWall.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FamilyWall.Infrastructure;

/// <summary>
/// Helper to manage database migrations and schema updates
/// </summary>
public static class DbMigrationHelper
{
    public static async Task EnsureSchemaUpdatedAsync(
        AppDbContext dbContext,
        ILogger? logger = null)
    {
        try
        {
            // Check if database exists
            var canConnect = await dbContext.Database.CanConnectAsync();

            if (!canConnect)
            {
                logger?.LogInformation("Database doesn't exist, creating from scratch...");
                await dbContext.Database.EnsureCreatedAsync();
                logger?.LogInformation("Database created successfully");
                return;
            }

            // Check if we need to recreate the database due to schema changes
            var needsRecreation = await NeedsSchemaUpdateAsync(dbContext, logger);

            if (needsRecreation)
            {
                logger?.LogWarning("Database schema is outdated. Recreating database...");
                logger?.LogWarning("All existing data will be lost!");

                // Delete and recreate
                await dbContext.Database.EnsureDeletedAsync();
                await dbContext.Database.EnsureCreatedAsync();

                logger?.LogInformation("Database recreated with new schema");
            }
            else
            {
                logger?.LogInformation("Database schema is up to date");
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error ensuring database schema is updated");
            throw;
        }
    }

    private static async Task<bool> NeedsSchemaUpdateAsync(AppDbContext dbContext, ILogger? logger)
    {
        try
        {
            // Try to query with new columns to see if they exist
            var testQuery = dbContext.CalendarEvents
                .Select(e => new
                {
                    e.Id,
                    e.CalendarId, // New column
                    e.IsRecurring, // New column
                    e.Organizer, // New column
                    e.LastSyncUtc // New column
                })
                .Take(1);

            await testQuery.ToListAsync();

            // If we got here, the columns exist
            return false;
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("no such column"))
        {
            logger?.LogWarning("Detected missing columns: {Message}", ex.Message);
            return true;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error checking schema version");
            // If we can't determine, safer to not recreate
            return false;
        }
    }

    public static async Task<Dictionary<string, object>> GetDatabaseInfoAsync(AppDbContext dbContext)
    {
        var info = new Dictionary<string, object>();

        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync();
            info["CanConnect"] = canConnect;

            if (canConnect)
            {
                var eventCount = await dbContext.CalendarEvents.CountAsync();
                var calendarCount = await dbContext.CalendarConfigurations.CountAsync();

                info["EventCount"] = eventCount;
                info["CalendarCount"] = calendarCount;
                info["DatabasePath"] = dbContext.Database.GetConnectionString() ?? "Unknown";
            }
        }
        catch (Exception ex)
        {
            info["Error"] = ex.Message;
        }

        return info;
    }
}
