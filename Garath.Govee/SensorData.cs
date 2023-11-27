using System.Text.Json.Serialization;

namespace Garath.Govee;

public class SensorData
{
    public DateTimeOffset Timestamp { get; }
    public string Address { get; }

    public int? ReceivedSignalStrength { get; set; }
    public float? TemperatureCelsius { get; set; }
    public float? Humidity { get; set; }
    public int? Battery { get; set; }

    public SensorData(DateTimeOffset timestamp, string address)
    {
        Timestamp = timestamp;
        Address = address;
    }
}

[JsonSerializable(typeof(SensorData))]
[JsonSerializable(typeof(IEnumerable<SensorData>))]
public partial class SensorDataSerializerContext : JsonSerializerContext
{
    
}