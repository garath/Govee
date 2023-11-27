using System.Net.Http.Json;

namespace Garath.Govee;

public class SensorApiClient
{
    private readonly HttpClient _client;

    public SensorApiClient(HttpClient client)
    {
        _client = client;
    }

    public async Task SendSensorData(IEnumerable<SensorData> data, CancellationToken cancellationToken)
    {
        using HttpResponseMessage message = await _client.PostAsJsonAsync(
            requestUri: "/api/govee", 
            @value: data, 
            jsonTypeInfo: SensorDataSerializerContext.Default.IEnumerableSensorData, 
            cancellationToken: cancellationToken);
    }
}
