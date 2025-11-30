using FamilyWall.Integrations.Graph;
using Microsoft.Extensions.Logging;

namespace FamilyWall.Services;

public class GraphSyncStrategy : ISyncStrategy
{
    public string Source => "Graph";

    private readonly IGraphClient _graphClient;
    private readonly ILogger<GraphSyncStrategy> _logger;

    public GraphSyncStrategy(IGraphClient graphClient, ILogger<GraphSyncStrategy> logger)
    {
        _graphClient = graphClient;
        _logger = logger;
    }

    public async Task<List<GraphEvent>> FetchEventsAsync(
        string calendarId, DateTime start, DateTime end, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetching events for calendar {CalendarId} from {Start:d} to {End:d}",
            calendarId, start, end);

        var events = await _graphClient.GetCalendarEventsAsync(calendarId, start, end, cancellationToken);

        _logger.LogDebug("Fetched {Count} events for calendar {CalendarId}", events.Count, calendarId);
        return events;
    }
}
