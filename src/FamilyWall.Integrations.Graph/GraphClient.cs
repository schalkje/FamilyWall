using FamilyWall.Core.Abstractions;
using FamilyWall.Core.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;

namespace FamilyWall.Integrations.Graph;

/// <summary>
/// Client for Microsoft Graph API (photos, calendar, contacts) using Device Code flow.
/// </summary>
public interface IGraphClient
{
    Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default);
    Task<bool> AuthenticateAsync(Func<string, Task> deviceCodeCallback, CancellationToken cancellationToken = default);
    Task<List<GraphPhoto>> GetRecentPhotosAsync(int count, CancellationToken cancellationToken = default);
    Task<List<GraphCalendar>> GetCalendarsAsync(CancellationToken cancellationToken = default);
    Task<List<GraphEvent>> GetCalendarEventsAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default);
    Task<List<GraphEvent>> GetCalendarEventsAsync(string calendarId, DateTime start, DateTime end, CancellationToken cancellationToken = default);
    Task SignOutAsync(CancellationToken cancellationToken = default);
}

public class GraphClient : IGraphClient
{
    private readonly GraphSettings _settings;
    private readonly ITokenStore _tokenStore;
    private readonly ILogger<GraphClient> _logger;
    internal IPublicClientApplication? _publicClientApp;
    private Microsoft.Graph.GraphServiceClient? _graphClient;

    public GraphClient(
        IOptions<AppSettings> appSettings,
        ITokenStore tokenStore,
        ILogger<GraphClient> logger)
    {
        _settings = appSettings.Value.Graph;
        _tokenStore = tokenStore;
        _logger = logger;
    }

    private Task InitializeClientAsync(CancellationToken cancellationToken = default)
    {
        if (_publicClientApp == null)
        {
            _publicClientApp = PublicClientApplicationBuilder
                .Create(_settings.ClientId)
                .WithTenantId(_settings.TenantId)
                .WithRedirectUri("http://localhost") // Required for device code flow
                .Build();
        }

        if (_graphClient == null)
        {
            var authProvider = new DeviceCodeAuthenticationProvider(this, _settings.Scopes);
            _graphClient = new Microsoft.Graph.GraphServiceClient(authProvider);
        }

        return Task.CompletedTask;
    }

    public async Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_settings.ClientId))
        {
            _logger.LogWarning("Graph ClientId not configured");
            return false;
        }

        var token = await _tokenStore.GetTokenAsync("Graph", string.Join(" ", _settings.Scopes), cancellationToken);
        return !string.IsNullOrEmpty(token);
    }

    public async Task<bool> AuthenticateAsync(Func<string, Task> deviceCodeCallback, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_settings.ClientId))
            {
                _logger.LogError("Graph ClientId is not configured in appsettings.json");
                return false;
            }

            await InitializeClientAsync(cancellationToken);

            var result = await _publicClientApp!.AcquireTokenWithDeviceCode(
                _settings.Scopes,
                async deviceCodeResult =>
                {
                    await deviceCodeCallback(deviceCodeResult.Message);
                })
                .ExecuteAsync(cancellationToken);

            // Store the refresh token (MSAL handles caching internally, but we also persist it)
            await _tokenStore.SetTokenAsync("Graph", string.Join(" ", _settings.Scopes), result.AccessToken, cancellationToken);

            _logger.LogInformation("Successfully authenticated with Microsoft Graph");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to authenticate with Microsoft Graph");
            return false;
        }
    }

    public async Task<List<GraphPhoto>> GetRecentPhotosAsync(int count, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            // Note: This is a placeholder implementation. For production, you'd want to:
            // 1. Use the /me/photos endpoint if available
            // 2. Search for specific folders like Pictures or Photos
            // 3. Filter by MIME type
            // For now, just return empty list as photo sync is not the focus of Phase 4
            _logger.LogInformation("Photo fetching from OneDrive is not yet implemented");
            return new List<GraphPhoto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch photos from Microsoft Graph");
            return new List<GraphPhoto>();
        }
    }

    public async Task<List<GraphCalendar>> GetCalendarsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        var calendars = await _graphClient!.Me.Calendars
            .GetAsync(requestConfig =>
            {
                requestConfig.QueryParameters.Top = 50;
                requestConfig.QueryParameters.Select = new[] { "id", "name", "owner", "canEdit", "isDefaultCalendar" };
            }, cancellationToken);

        var graphCalendars = new List<GraphCalendar>();
        if (calendars?.Value != null)
        {
            foreach (var cal in calendars.Value)
            {
                graphCalendars.Add(new GraphCalendar(
                    cal.Id!,
                    cal.Name ?? "(Unnamed calendar)",
                    cal.Owner?.Name ?? cal.Owner?.Address,
                    cal.CanEdit ?? false,
                    cal.IsDefaultCalendar ?? false
                ));
            }
        }

        _logger.LogInformation("Fetched {Count} calendars from Microsoft Graph", graphCalendars.Count);
        return graphCalendars;
    }

    public async Task<List<GraphEvent>> GetCalendarEventsAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        // Get events from all configured calendars or all calendars if none specified
        if (_settings.CalendarIds.Count == 0)
        {
            // Use default calendar
            return await GetCalendarEventsAsync("calendar", start, end, cancellationToken);
        }
        else
        {
            // Merge events from all selected calendars
            var allEvents = new List<GraphEvent>();
            foreach (var calendarId in _settings.CalendarIds)
            {
                var events = await GetCalendarEventsAsync(calendarId, start, end, cancellationToken);
                allEvents.AddRange(events);
            }
            return allEvents.OrderBy(e => e.Start).ToList();
        }
    }

    public async Task<List<GraphEvent>> GetCalendarEventsAsync(string calendarId, DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            // Use specific calendar or default calendar
            EventCollectionResponse? events;
            if (calendarId == "calendar")
            {
                events = await _graphClient!.Me.Calendar.CalendarView
                    .GetAsync(requestConfig =>
                    {
                        requestConfig.QueryParameters.StartDateTime = start.ToString("o");
                        requestConfig.QueryParameters.EndDateTime = end.ToString("o");
                        requestConfig.QueryParameters.Orderby = new[] { "start/dateTime" };
                        requestConfig.QueryParameters.Top = 100;
                    }, cancellationToken);
            }
            else
            {
                events = await _graphClient!.Me.Calendars[calendarId].CalendarView
                    .GetAsync(requestConfig =>
                    {
                        requestConfig.QueryParameters.StartDateTime = start.ToString("o");
                        requestConfig.QueryParameters.EndDateTime = end.ToString("o");
                        requestConfig.QueryParameters.Orderby = new[] { "start/dateTime" };
                        requestConfig.QueryParameters.Top = 100;
                    }, cancellationToken);
            }

            var graphEvents = new List<GraphEvent>();
            if (events?.Value != null)
            {
                foreach (var evt in events.Value)
                {
                    var isBirthday = evt.Categories?.Contains("Birthday") == true ||
                                     evt.Subject?.ToLower().Contains("birthday") == true;

                    graphEvents.Add(new GraphEvent(
                        evt.Id!,
                        evt.Subject ?? "(No subject)",
                        evt.Start?.DateTime != null ? DateTime.Parse(evt.Start.DateTime) : DateTime.MinValue,
                        evt.End?.DateTime != null ? DateTime.Parse(evt.End.DateTime) : DateTime.MinValue,
                        evt.IsAllDay ?? false,
                        isBirthday
                    ));
                }
            }

            _logger.LogInformation("Fetched {Count} calendar events from Microsoft Graph", graphEvents.Count);
            return graphEvents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch calendar events from Microsoft Graph");
            return new List<GraphEvent>();
        }
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _tokenStore.RemoveTokenAsync("Graph", string.Join(" ", _settings.Scopes), cancellationToken);

            if (_publicClientApp != null)
            {
                var accounts = await _publicClientApp.GetAccountsAsync();
                foreach (var account in accounts)
                {
                    await _publicClientApp.RemoveAsync(account);
                }
            }

            _graphClient = null;
            _logger.LogInformation("Signed out from Microsoft Graph");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign out from Microsoft Graph");
        }
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        await InitializeClientAsync(cancellationToken);

        // Try to acquire token silently first
        try
        {
            var accounts = await _publicClientApp!.GetAccountsAsync();
            if (accounts.Any())
            {
                var result = await _publicClientApp.AcquireTokenSilent(_settings.Scopes, accounts.First())
                    .ExecuteAsync(cancellationToken);
                await _tokenStore.SetTokenAsync("Graph", string.Join(" ", _settings.Scopes), result.AccessToken, cancellationToken);
                return;
            }
            else
            {
                // No accounts in MSAL cache, user needs to authenticate
                throw new InvalidOperationException("Not authenticated. Call AuthenticateAsync first.");
            }
        }
        catch (MsalUiRequiredException)
        {
            // Silent acquisition failed, user needs to authenticate
            throw new InvalidOperationException("Authentication expired. Please sign in again.");
        }
    }
}

/// <summary>
/// Custom authentication provider for Kiota-based Graph SDK
/// </summary>
internal class DeviceCodeAuthenticationProvider : IAuthenticationProvider
{
    private readonly GraphClient _graphClient;
    private readonly string[] _scopes;

    public DeviceCodeAuthenticationProvider(GraphClient graphClient, string[] scopes)
    {
        _graphClient = graphClient;
        _scopes = scopes;
    }

    public async Task AuthenticateRequestAsync(
        Microsoft.Kiota.Abstractions.RequestInformation request,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        // Get token from MSAL
        if (_graphClient._publicClientApp != null)
        {
            try
            {
                var accounts = await _graphClient._publicClientApp.GetAccountsAsync();
                AuthenticationResult result;

                if (accounts.Any())
                {
                    result = await _graphClient._publicClientApp
                        .AcquireTokenSilent(_scopes, accounts.First())
                        .ExecuteAsync(cancellationToken);
                }
                else
                {
                    throw new InvalidOperationException("Not authenticated. Call AuthenticateAsync first.");
                }

                request.Headers.Add("Authorization", $"Bearer {result.AccessToken}");
            }
            catch (MsalUiRequiredException)
            {
                throw new InvalidOperationException("User authentication required. Please call AuthenticateAsync first.");
            }
        }
    }
}

public record GraphPhoto(string Id, string Name, string? ThumbnailUrl, DateTime? TakenDateTime);
public record GraphCalendar(string Id, string Name, string? Owner, bool CanEdit, bool IsDefaultCalendar);
public record GraphEvent(string Id, string Subject, DateTime Start, DateTime End, bool IsAllDay, bool IsBirthday);
