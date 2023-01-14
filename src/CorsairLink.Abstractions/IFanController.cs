namespace CorsairLink;

public interface IFanController : IFanReader
{
    void SetFanPower(int channelId, int percent);
}