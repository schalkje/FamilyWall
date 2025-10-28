namespace FamilyWall.Core.Abstractions;

/// <summary>
/// Provides the path for the local SQLite database.
/// </summary>
public interface IDbPathProvider
{
    string GetDbPath();
}
