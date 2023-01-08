namespace CorsairLink;

public class FanConfiguration
{
    public FanConfiguration(IEnumerable<FanChannel> channels)
    {
        Channels = channels.ToArray();
    }

    public FanChannel[] Channels { get; }
}
