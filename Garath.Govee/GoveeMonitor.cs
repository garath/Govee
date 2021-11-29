using HashtagChris.DotNetBlueZ;
using HashtagChris.DotNetBlueZ.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Channels;

namespace Garath.Govee;

public class GoveeMonitorConfiguration
{
    public IList<string> AddressesToMonitor { get; set; } = new List<string>();
}

public sealed class GoveeMonitor : IHostedService, IDisposable
{
    private readonly ILogger<GoveeMonitor> _logger;
    private readonly Adapter _adapter;
    private readonly GoveeMonitorConfiguration _configuration;
    private readonly ChannelWriter<SensorData> _writer;

    private Task? _monitorTask;
    private readonly CancellationTokenSource _stoppingTokenSource = new();
    private readonly Dictionary<string, IDisposable> disposableWatchers = new();

    public GoveeMonitor(ILogger<GoveeMonitor> logger, Adapter adapter, ChannelWriter<SensorData> writer, IOptions<GoveeMonitorConfiguration> configuration)
    {
        _logger = logger;
        _adapter = adapter;
        _writer = writer;
        _configuration = configuration.Value;
    }

    public void Dispose()
    {
        _stoppingTokenSource.Dispose();

        foreach (IDisposable watcher in disposableWatchers.Values)
        {
            watcher.Dispose();
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting Monitor task");
        _logger.LogDebug("Monitoring addresses: {MonitoredAddresses}", string.Join(", ", _configuration.AddressesToMonitor));

        _monitorTask = MonitorTask(_stoppingTokenSource.Token);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _stoppingTokenSource.Cancel();
        return _monitorTask ?? Task.CompletedTask;
    }

    private async Task MonitorTask(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Interrogating known devices");
        IReadOnlyList<Device> devices = await _adapter.GetDevicesAsync();
        _logger.LogTrace("Found {ScannedDeviceFoundCount} devices in scan", devices.Count);

        foreach (Device device in devices)
        {
            Device1Properties deviceProperties = await device.GetAllAsync(); // TODO: Need just address?

            // Only look at devices on the watch list
            if (!_configuration.AddressesToMonitor.Contains(deviceProperties.Address))
            {
                _logger.LogTrace("Ignoring device {DeviceAddress}", deviceProperties.Address);
                continue;
            }

            _logger.LogTrace("Monitoring device {DeviceAddress}", deviceProperties.Address);
            IDisposable watcher = await device.WatchPropertiesAsync(changes => PropertiesWatcher(deviceProperties.Address, changes));
            disposableWatchers.Add(deviceProperties.Address, watcher);
        }

        _logger.LogTrace("Starting discovery");
        await _adapter.StartDiscoveryAsync();
        try
        {
            await Task.Delay(-1, stoppingToken);
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == stoppingToken)
        {
            _logger.LogInformation("Stopping monitor");
            await _adapter.StopDiscoveryAsync();
            _writer.Complete();
        }
    }

    private void PropertiesWatcher(string address, Tmds.DBus.PropertyChanges changes)
    {
        using IDisposable addressScope = _logger.BeginScope(KeyValuePair.Create("Address", address));
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        foreach ((string key, object value) in changes.Changed)
        {
            if (value is short valueShort)
            {
                if (key == "RSSI")
                {
                    _logger.LogDebug("RSSI: {RSSI}", valueShort);

                    SensorData sensorData = new(timestamp, address)
                    {
                        ReceivedSignalStrength = valueShort
                    };

                    bool writeSuccessful;
                    do
                    {
                        writeSuccessful = _writer.TryWrite(sensorData);
                    } while (!writeSuccessful);
                }
                else
                {
                    _logger.LogDebug("Key: {Key}, Value: {Value}", key, valueShort);
                }
            }
            else if (value is Dictionary<ushort, object> valueDictionary)
            {
                foreach ((ushort subkey, object subvalue) in valueDictionary)
                {
                    if (subvalue is byte[] subvalueBytes)
                    {
                        if (subkey == 0xEC88)
                        {
                            // TODO: subValueBytes[0] and subValueBytes[5] are zero in this format. Assert?

                            int temperatureAndHumidity = subvalueBytes[1] << 16 | subvalueBytes[2] << 8 | subvalueBytes[3];
                            float temperatureInCelsius = (float)temperatureAndHumidity / 10000;
                            float humidity = (float)(temperatureAndHumidity % 1000) / 10;

                            int battery = subvalueBytes[4];

                            _logger.LogDebug("Temperature: {TemperatureCelsius}°C", temperatureInCelsius);
                            _logger.LogDebug("Humidity: {Humidity}%", humidity);
                            _logger.LogDebug("Battery: {Battery}%", battery);

                            SensorData sensorData = new(timestamp, address)
                            {
                                TemperatureCelsius = temperatureInCelsius,
                                Humidity = humidity,
                                Battery = battery
                            };

                            bool writeSuccessful;
                            do
                            {
                                writeSuccessful = _writer.TryWrite(sensorData);
                            } while (!writeSuccessful);
                        }
                        else
                        {
                            _logger.LogDebug("Key: {Key}, SubKey: {SubKey}, SubValue: {SubValue}", key, subkey, BitConverter.ToString(subvalueBytes));
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Unrecognized subvalue type {UnrecognizedSubvalueType}", subvalue.GetType());
                    }
                }
            }
            else
            {
                _logger.LogDebug("No handler for value");
            }
        }
    }
}
