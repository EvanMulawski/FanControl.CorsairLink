using System.Buffers.Binary;

namespace CorsairLink.Devices.HydroPlatinum;

public class HydroPlatinumDataReader
{
    protected readonly int _fanCount;

    public HydroPlatinumDataReader(int fanCount)
    {
        _fanCount = fanCount;
    }

    public virtual HydroPlatinumDeviceState GetState(ReadOnlySpan<byte> packet)
    {
        var fwMajor = packet[2] >> 4;
        var fwMinor = packet[2] & 15;
        var fwRevision = (int)packet[3];
        var liquidTempRaw = (double)BinaryPrimitives.ReadInt16LittleEndian(packet.Slice(7, 2));

        var state = new HydroPlatinumDeviceState
        {
            SequenceNumber = packet[1],
            Status = (DeviceStatus)packet[4],
            FirmwareVersionMajor = fwMajor,
            FirmwareVersionMinor = fwMinor,
            FirmwareVersionRevision = fwRevision,
            LiquidTempCelsius = (int)(liquidTempRaw / 25.6 + 0.5) / 10f,
            PumpMode = (PumpMode)packet[24],
            PumpRpm = BinaryPrimitives.ReadInt16LittleEndian(packet.Slice(29, 2)),
            FanRpm = new int[_fanCount]
        };

        if (_fanCount >= 1)
        {
            state.FanRpm[0] = BinaryPrimitives.ReadInt16LittleEndian(packet.Slice(15, 2));
        }

        if (_fanCount >= 2)
        {
            state.FanRpm[1] = BinaryPrimitives.ReadInt16LittleEndian(packet.Slice(22, 2));
        }

        if (_fanCount >= 3)
        {
            state.FanRpm[2] = BinaryPrimitives.ReadInt16LittleEndian(packet.Slice(43, 2));
        }

        return state;
    }

    public virtual byte GetChecksumByte(ReadOnlySpan<byte> packet)
    {
        return packet[packet.Length - 1];
    }

    public virtual byte CalculateChecksumByte(ReadOnlySpan<byte> packet)
    {
        var bytesForChecksum = packet.Slice(1, packet.Length - 1 - 1);
        return Crc8Ccitt.CalculateChecksumByte(bytesForChecksum);
    }
}
