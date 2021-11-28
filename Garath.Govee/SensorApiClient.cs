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
            using HttpResponseMessage message = await _client.PostAsJsonAsync("/api/govee", data, cancellationToken: cancellationToken);
        }
    }
