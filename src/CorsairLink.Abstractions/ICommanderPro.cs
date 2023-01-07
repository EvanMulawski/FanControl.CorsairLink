namespace CorsairLink.Core;

public interface ICommanderPro
{
    Task<string> GetFirmwareVersionAsync(CancellationToken cancellationToken);

    Task<int> GetFanRpmAsync(int channelId, CancellationToken cancellationToken);

    Task SetFanRpmAsync(int channelId, int speedPercent, CancellationToken cancellationToken);
}
