using Garath.Govee.SiteApp.Shared;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;

namespace Garath.SensorApi;

public class PgSensorDataProviderConfiguration
{
    public string? ConnectionString { get; set; }
}

public class PgSensorDataProvider
{
    private readonly PgSensorDataProviderConfiguration _configuration;

    public PgSensorDataProvider(IOptions<PgSensorDataProviderConfiguration> configuration)
    {
        _configuration = configuration.Value;

        AppContext.SetSwitch("Npgsql.EnableSqlRewriting", false);
    }

    public async IAsyncEnumerable<SensorData> Get([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (_configuration.ConnectionString == null)
            throw new InvalidOperationException("ConfigurationString is null");

        using NpgsqlConnection connection = new(_configuration.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using NpgsqlCommand command = connection.CreateCommand();

        command.CommandText =
            "SELECT GOVEE.TIMESTAMP, " +
                "GOVEE.ADDRESS, " +
                "TEMP_C, " +
                "HUMIDITY, " +
                "BATTERY, " +
                "(SELECT RSSI " +
                    "FROM GOVEE AS I " +
                    "WHERE I.ADDRESS = GOVEE.ADDRESS " +
                        "AND RSSI IS NOT NULL " +
                    "ORDER BY TIMESTAMP DESC " +
                    "LIMIT 1) " +
            "FROM GOVEE " +
            "INNER JOIN " +
                "(SELECT ADDRESS, MAX(TIMESTAMP) AS TIMESTAMP " +
                    "FROM GOVEE " +
                    "WHERE RSSI IS NULL " +
                    "GROUP BY ADDRESS) AS L ON GOVEE.ADDRESS = L.ADDRESS " +
                        "AND GOVEE.TIMESTAMP = L.TIMESTAMP " +
                        "AND RSSI IS NULL";

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            DateTimeOffset timestamp = reader.GetFieldValue<DateTimeOffset>(0);
            PhysicalAddress address = (PhysicalAddress)reader.GetValue(1);
            float? temperature = reader.GetValue(2) == DBNull.Value ? null : (float)reader.GetValue(2);
            float? humidity = reader.GetValue(3) == DBNull.Value ? null : (float)reader.GetValue(3);
            int? battery = reader.GetValue(4) == DBNull.Value ? null : (int)reader.GetValue(4);
            int? rssi = reader.GetValue(5) == DBNull.Value ? null : (int)reader.GetValue(5);

            SensorData data = new(timestamp, address.ToString())
            {
                TemperatureCelsius = temperature,
                Humidity = humidity,
                Battery = battery,
                ReceivedSignalStrength = rssi
            };

            yield return data;
        }

        yield break;
    }

    public async Task AddRange(IEnumerable<SensorData> data, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = new(_configuration.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using NpgsqlCommand command = new(
                "INSERT INTO govee (timestamp, address, rssi, temp_c, humidity, battery) " +
                "VALUES ($1, $2, $3, $4, $5, $6)"
            , connection);

        NpgsqlParameter timestampParameter = new(null, NpgsqlTypes.NpgsqlDbType.TimestampTz);
        NpgsqlParameter addressParameter = new(null, NpgsqlTypes.NpgsqlDbType.MacAddr8);
        NpgsqlParameter rssiParameter = new(null, NpgsqlTypes.NpgsqlDbType.Integer) { IsNullable = true };
        NpgsqlParameter temperatureParameter = new(null, NpgsqlTypes.NpgsqlDbType.Real) { IsNullable = true };
        NpgsqlParameter humidityParameter = new(null, NpgsqlTypes.NpgsqlDbType.Real) { IsNullable = true };
        NpgsqlParameter batteryParameter = new(null, NpgsqlTypes.NpgsqlDbType.Integer) { IsNullable = true };

        command.Parameters.Add(timestampParameter);
        command.Parameters.Add(addressParameter);
        command.Parameters.Add(rssiParameter);
        command.Parameters.Add(temperatureParameter);
        command.Parameters.Add(humidityParameter);
        command.Parameters.Add(batteryParameter);

        foreach (SensorData d in data)
        {
            cancellationToken.ThrowIfCancellationRequested();

            timestampParameter.Value = d.Timestamp;
            addressParameter.Value = PhysicalAddress.Parse(d.Address);
            rssiParameter.Value = d.ReceivedSignalStrength == null ? DBNull.Value : d.ReceivedSignalStrength;
            temperatureParameter.Value = d.TemperatureCelsius == null ? DBNull.Value : d.TemperatureCelsius;
            humidityParameter.Value = d.Humidity == null ? DBNull.Value : d.Humidity;
            batteryParameter.Value = d.Battery == null ? DBNull.Value : d.Battery;

            await command.ExecuteNonQueryAsync(CancellationToken.None);
        }
    }
}
