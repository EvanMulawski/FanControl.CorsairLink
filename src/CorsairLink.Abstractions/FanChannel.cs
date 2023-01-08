namespace CorsairLink;

public class FanChannel
{
    public FanChannel(int channelId, FanMode mode)
    {
        ChannelId = channelId;
        Mode = mode;
    }

    public int ChannelId { get; }
    public FanMode Mode { get; }
}