using FamilyWall.Core.Abstractions;

namespace FamilyWall.Infrastructure;

/// <summary>
/// Provides the database path for SQLite in the app data directory.
/// </summary>
public class DbPathProvider : IDbPathProvider
{
    private readonly string _appDataPath;

    public DbPathProvider(string appDataPath)
    {
        _appDataPath = appDataPath;
    }

    public string GetDbPath()
    {
        var directory = Path.Combine(_appDataPath, "Database");
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        return Path.Combine(directory, "familywall.db");
    }
}
