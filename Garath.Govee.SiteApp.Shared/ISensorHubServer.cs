namespace Garath.Govee.SiteApp.Shared;

/// <summary>
/// SignalR methods that clients may call on the server
/// </summary>
public interface ISensorHubServer
{
    /// <summary>
    /// Ask the server to send the current sensor data. Useful for initial connect.
    /// </summary>
    public Task SendFirstData();
}