namespace CorsairLink;

public class TemperatureSensorChannel
{
    public TemperatureSensorChannel(int channelId, TemperatureSensorStatus status)
    {
        ChannelId = channelId;
        Status = status;
    }

    public int ChannelId { get; }
    public TemperatureSensorStatus Status { get; }
}
