using System.Net.WebSockets;

namespace FamilyWall.Integrations.HA;

/// <summary>
/// Stub client for Home Assistant WebSocket + REST API.
/// </summary>
public interface IHomeAssistantClient
{
    Task ConnectAsync(string baseUrl, string token, CancellationToken cancellationToken = default);
    Task<List<HAEntity>> GetEntitiesAsync(CancellationToken cancellationToken = default);
    Task CallServiceAsync(string domain, string service, object? data = null, CancellationToken cancellationToken = default);
    Task DisconnectAsync();
}

public class HomeAssistantClient : IHomeAssistantClient
{
    private ClientWebSocket? _webSocket;

    public async Task ConnectAsync(string baseUrl, string token, CancellationToken cancellationToken = default)
    {
        // TODO: Implement WebSocket connection to /api/websocket
        // 1. Connect to ws://{baseUrl}/api/websocket
        // 2. Authenticate with access token
        // 3. Subscribe to state_changed events
        await Task.CompletedTask;
    }

    public async Task<List<HAEntity>> GetEntitiesAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement via WebSocket get_states or REST /api/states
        await Task.CompletedTask;
        return new List<HAEntity>();
    }

    public async Task CallServiceAsync(string domain, string service, object? data = null, CancellationToken cancellationToken = default)
    {
        // TODO: Implement via REST POST /api/services/{domain}/{service}
        // Example: domain="light", service="turn_on", data={entity_id: "light.living_ceiling", brightness_pct: 45}
        await Task.CompletedTask;
    }

    public async Task DisconnectAsync()
    {
        if (_webSocket != null && _webSocket.State == WebSocketState.Open)
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
        }
    }
}

public record HAEntity(string EntityId, string State, Dictionary<string, object>? Attributes);
