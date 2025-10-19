namespace FamilyWall.Integrations.HA;

/// <summary>
/// Stub MQTT client factory for publishing presence/state and optional Home Assistant Discovery.
/// </summary>
public interface IMqttClientFactory
{
    Task PublishAsync(string topic, string payload, CancellationToken cancellationToken = default);
    Task ConnectAsync(string broker, int port, CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}

public class MqttClientFactory : IMqttClientFactory
{
    public Task PublishAsync(string topic, string payload, CancellationToken cancellationToken = default)
    {
        // TODO: Implement using MQTTnet v5 API
        // var factory = new MqttFactory();
        // var client = factory.CreateMqttClient();
        // await client.PublishAsync(...)
        return Task.CompletedTask;
    }

    public Task ConnectAsync(string broker, int port, CancellationToken cancellationToken = default)
    {
        // TODO: Connect to MQTT broker
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Disconnect
        return Task.CompletedTask;
    }
}
