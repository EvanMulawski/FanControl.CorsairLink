using System.Buffers.Binary;

namespace CorsairLink.Devices.HydroPlatinum;

public class OneComputerDataReader : HydroPlatinumDataReader
{
    public OneComputerDataReader()
        : base(fanCount: 1)
    {

    }

    public override HydroPlatinumDeviceState GetState(ReadOnlySpan<byte> packet)
    {
        var baseState = base.GetState(packet);

        var state = new OneComputerDeviceState
        {
            FanRpm = baseState.FanRpm[0],
            FirmwareVersionMajor = baseState.FirmwareVersionMajor,
            FirmwareVersionMinor = baseState.FirmwareVersionMinor,
            FirmwareVersionRevision = baseState.FirmwareVersionRevision,
            LiquidTempCelsius = baseState.LiquidTempCelsius,
            PumpMode = baseState.PumpMode,
            PumpRpm = baseState.PumpRpm,
            SequenceNumber = baseState.SequenceNumber,
        };

        var gpuTempRaw = (double)BinaryPrimitives.ReadInt16LittleEndian(packet.Slice(49, 2));
        state.GpuLiquidTempCelsius = (int)(gpuTempRaw / 25.6 + 0.5) / 10f;
        state.GpuPumpRpm = BinaryPrimitives.ReadInt16LittleEndian(packet.Slice(22, 2));

        return state;
    }
}
