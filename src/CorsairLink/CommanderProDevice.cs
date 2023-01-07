using CorsairLink.Core;
using Device.Net;
using System.Buffers.Binary;

namespace CorsairLink;

public sealed class CommanderProDevice : ICommanderPro
{
    private readonly IDevice _device;

    public CommanderProDevice(IDevice device)
    {
        _device = device;
    }

    public async Task<string> GetFirmwareVersionAsync(CancellationToken cancellationToken = default)
    {
        var request = Utils.CreateRequest(HidDeviceCommands.CommanderPro.ReadFirmwareVersion);
        var result = await _device.WriteAndReadAsync(request, cancellationToken);

        var v1 = (int)result.Data[2];
        var v2 = (int)result.Data[3];
        var v3 = (int)result.Data[4];

        return $"{v1}.{v2}.{v3}";
    }

    public async Task<int> GetFanRpmAsync(int channelId, CancellationToken cancellationToken = default)
    {
        var request = Utils.CreateRequest(HidDeviceCommands.CommanderPro.ReadFanSpeed);
        request[2] = Convert.ToByte(Math.Clamp(channelId, 0, 5));
        var result = await _device.WriteAndReadAsync(request, cancellationToken);

        return BinaryPrimitives.ReadInt16BigEndian(result.Data.AsSpan()[2..]);
    }

    public async Task SetFanRpmAsync(int channelId, int rpm, CancellationToken cancellationToken = default)
    {
        var request = Utils.CreateRequest(HidDeviceCommands.CommanderPro.WriteFanSpeed);
        request[2] = Convert.ToByte(Math.Clamp(channelId, 0, 5));
        BinaryPrimitives.WriteInt16BigEndian(request.AsSpan()[3..], Convert.ToInt16(rpm));

        _ = await _device.WriteAndReadAsync(request, cancellationToken);
    }
}
