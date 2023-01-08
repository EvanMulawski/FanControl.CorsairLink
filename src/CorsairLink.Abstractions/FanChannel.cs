namespace CorsairLink;

public sealed class FanChannel
{
    public FanChannel(int channelId, FanMode mode)
    {
        ChannelId = channelId;
        Mode = mode;
        Name = $"{channelId + 1}";
    }

    public string Name { get; }
    public int ChannelId { get; }
    public FanMode Mode { get; }
}