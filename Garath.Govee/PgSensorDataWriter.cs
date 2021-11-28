using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using System.Diagnostics;
using System.Threading.Channels;

namespace Garath.Govee;

public sealed class PgSensorDataWriter : BackgroundService
{
    private readonly ILogger<PgSensorDataWriter> _logger;
    private readonly ChannelReader<SensorData> _reader;
    private readonly SensorApiClient _sensorApiClient;

    public PgSensorDataWriter(ILogger<PgSensorDataWriter> logger, ChannelReader<SensorData> reader, SensorApiClient sensorApiClient)
    {
        _logger = logger;
        _reader = reader;
        _sensorApiClient = sensorApiClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (SensorData data in _reader.ReadAllAsync(CancellationToken.None))
        {
            Activity activity = new Activity("PostNewSensorData").Start();

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
            finally
            {
                activity.Stop();
            }
        }

        _logger.LogInformation("Ending sensor send");
    }
}