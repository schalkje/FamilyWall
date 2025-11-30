using FamilyWall.Integrations.Graph;

namespace FamilyWall.Services;

public interface ISyncStrategy
{
    string Source { get; }
    Task<List<GraphEvent>> FetchEventsAsync(string calendarId, DateTime start, DateTime end, CancellationToken cancellationToken);
}
