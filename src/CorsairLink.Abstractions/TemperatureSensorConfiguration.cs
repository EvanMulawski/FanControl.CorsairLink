namespace CorsairLink;

public class TemperatureSensorConfiguration
{
    public TemperatureSensorConfiguration(IEnumerable<TemperatureSensorChannel> channels)
    {
        Channels = channels.ToArray();
    }

    public TemperatureSensorChannel[] Channels { get; }
}
