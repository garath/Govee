using Garath.Govee.SiteApp.Shared;
using System.Net.Http.Json;

namespace Garath.Govee.SiteApp.Client;

public class SensorApiClient
{
    private readonly HttpClient _httpClient;

    public SensorApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<SensorData>> GetSensorDataAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<SensorData>>("/api/govee", cancellationToken) 
            ?? Enumerable.Empty<SensorData>();
    }
}
