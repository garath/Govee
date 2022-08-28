namespace Garath.Govee.SiteApp.Shared;

/// <summary>
/// SignalR methods that the server may call on clients
/// </summary>
public interface ISensorHubClient
{
    public Task ReceiveData(IEnumerable<SensorData> data, CancellationToken cancellationToken = default);
}
