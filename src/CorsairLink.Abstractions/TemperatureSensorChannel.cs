namespace CorsairLink;

public class TemperatureSensorChannel
{
    public TemperatureSensorChannel(int channelId, TemperatureSensorStatus status)
    {
        ChannelId = channelId;
        Status = status;
        Name = $"{channelId + 1}";
    }

    public string Name { get; }
    public int ChannelId { get; }
    public TemperatureSensorStatus Status { get; }
}
