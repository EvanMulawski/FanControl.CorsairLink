using CorsairLink.Core;
using Device.Net;
using System.Buffers.Binary;

namespace CorsairLink
{
    public sealed class CommanderProDevice : ICommanderPro
    {
        private readonly IDevice _device;

        public CommanderProDevice(IDevice device)
        {
            _device = device;
        }

        public async Task InitializeDeviceAsync(CancellationToken cancellationToken = default)
        {
            if (!_device.IsInitialized)
            {
                await _device.InitializeAsync(cancellationToken);
            }
        }

        public async Task<string> GetFirmwareVersionAsync(CancellationToken cancellationToken = default)
        {
            var writeBuf = new byte[64];
            writeBuf[1] = 0x02;

            var result = await _device.WriteAndReadAsync(writeBuf, cancellationToken);

            var v1 = (int)result.Data[2];
            var v2 = (int)result.Data[3];
            var v3 = (int)result.Data[4];

            return $"{v1}.{v2}.{v3}";
        }

        public async Task<int> GetFanRpmAsync(int fanIndex, CancellationToken cancellationToken = default)
        {
            var writeBuf = new byte[64];
            writeBuf[1] = 0x21;
            writeBuf[2] = (byte)fanIndex;

            var result = await _device.WriteAndReadAsync(writeBuf, cancellationToken);

            return BinaryPrimitives.ReadInt16BigEndian(result.Data.AsSpan()[2..]);
        }
    }
}
