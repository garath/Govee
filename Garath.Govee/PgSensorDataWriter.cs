using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Polly;
using System.Net.NetworkInformation;
using System.Threading.Channels;

namespace Garath.Govee;

public sealed class PgSensorDataWriter : BackgroundService
{
    private readonly ILogger<PgSensorDataWriter> _logger;
    private readonly ChannelReader<SensorData> _reader;
    private readonly PgSensorDataWriterConfiguration _configuration;

    public PgSensorDataWriter(ILogger<PgSensorDataWriter> logger, ChannelReader<SensorData> reader, IOptions<PgSensorDataWriterConfiguration> configuration)
    {
        _logger = logger;
        _reader = reader;
        _configuration = configuration.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (SensorData data in _reader.ReadAllAsync())
        {
            try
            {
                await using var connection = new NpgsqlConnection(_configuration.ConnectionString);

                await Policy
                    .Handle<NpgsqlException>()
                    .WaitAndRetryForeverAsync(
                        durationProvider => TimeSpan.FromSeconds(60),
                        (exception, retryCount, context) => _logger.LogError(exception, "Failed to open database connection, retry attempt {RetryAttempt} in {RetryDelay}", retryCount, context))
                    .ExecuteAsync(async cancellationToken => await connection.OpenAsync(cancellationToken), stoppingToken);

                await using NpgsqlCommand command = new NpgsqlCommand(
                        "INSERT INTO govee (timestamp, address, rssi, temp_c, humidity, battery) " +
                        "VALUES (@timestamp, @address, @rssi, @temp_c, @humidity, @battery)"
                    , connection);

                command.Parameters.AddWithValue("timestamp", data.Timestamp);
                command.Parameters.AddWithValue("address", PhysicalAddress.Parse(data.Address.Replace(':', '-')));

                if (data.ReceivedSignalStrength == null)
                {
                    command.Parameters.AddWithValue("rssi", DBNull.Value);
                }
                else
                {
                    command.Parameters.AddWithValue("rssi", data.ReceivedSignalStrength);
                }

                if (data.TemperatureCelsius == null)
                {
                    command.Parameters.AddWithValue("temp_c", DBNull.Value);
                }
                else
                {
                    command.Parameters.AddWithValue("temp_c", data.TemperatureCelsius);
                }

                if (data.Humidity == null)
                {
                    command.Parameters.AddWithValue("humidity", DBNull.Value);
                }
                else
                {
                    command.Parameters.AddWithValue("humidity", data.Humidity);
                }

                if (data.Battery == null)
                {
                    command.Parameters.AddWithValue("battery", DBNull.Value);
                }
                else
                {
                    command.Parameters.AddWithValue("battery", data.Battery);
                }

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Caught exception while trying to write sensor data");
            }
        }

        _logger.LogInformation("Ending sensor database send");
    }
}
