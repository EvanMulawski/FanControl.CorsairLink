namespace CorsairLink.Core
{
    public interface ICommanderPro
    {
        Task<string> GetFirmwareVersionAsync(CancellationToken cancellationToken);

        Task<int> GetFanRpmAsync(int fanIndex, CancellationToken cancellationToken);
    }
}
