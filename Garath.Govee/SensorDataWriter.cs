using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using System.Threading.Channels;

namespace Garath.Govee;

public sealed class SensorDataWriter : BackgroundService
{
    private readonly ILogger<SensorDataWriter> _logger;
    private readonly ChannelReader<SensorData> _reader;
    private readonly SensorApiClient _sensorApiClient;

    public SensorDataWriter(ILogger<SensorDataWriter> logger, ChannelReader<SensorData> reader, SensorApiClient sensorApiClient)
    {
        _logger = logger;
        _reader = reader;
        _sensorApiClient = sensorApiClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (SensorData data in _reader.ReadAllAsync(CancellationToken.None))
        {
            try
            {
                await Policy
                    .Handle<HttpRequestException>()
                    .WaitAndRetryForeverAsync(
                        durationProvider => TimeSpan.FromSeconds(60),
                        (exception, retryCount, context) => _logger.LogError(exception, "Failed to send sensor data, retry attempt {RetryAttempt} in {RetryDelay}", retryCount, context))
                    .ExecuteAsync(async cancellationToken => await _sensorApiClient.SendSensorData(new[] { data }, cancellationToken), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Caught exception while trying to write sensor data");
            }
        }

        _logger.LogInformation("Ending sensor send");
    }
}
