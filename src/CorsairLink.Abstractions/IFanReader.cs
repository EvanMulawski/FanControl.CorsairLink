namespace CorsairLink
{
    public interface IFanReader
    {
        FanConfiguration GetFanConfiguration();
        int GetFanRpm(int channelId);
    }
}