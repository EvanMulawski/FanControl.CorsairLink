using CorsairLink.Devices.HydroPlatinum;
using System.Buffers.Binary;

namespace CorsairLink.Devices;

public class OneComputerDevice : HydroPlatinumDevice
{
    private const int PUMP_CHANNEL_CPU = -1;
    private const int PUMP_CHANNEL_GPU = -2;
    private const int FAN_CHANNEL = 0;

    private bool _isGpuUsingPump;

    public OneComputerDevice(IHidDeviceProxy device, IDeviceGuardManager guardManager, ILogger logger)
        : base(device, guardManager, new HydroPlatinumDeviceOptions { FanChannelCount = 1 }, logger)
    {

    }

    protected override void Initialize()
    {
        OneComputerDeviceState state;
        using (_guardManager.AwaitExclusiveAccess())
        {
            state = ReadState();
        }

        _isGpuUsingPump = IsGpuUsingPump(state);

        _requestedChannelPower.Clear();
        SetChannelPower(FAN_CHANNEL, DEFAULT_SPEED_CHANNEL_POWER);
        SetChannelPower(PUMP_CHANNEL_CPU, DEFAULT_SPEED_CHANNEL_POWER);

        _speedSensors[FAN_CHANNEL] = new SpeedSensor("Fan", FAN_CHANNEL, state.FanRpm, supportsControl: true);
        _speedSensors[PUMP_CHANNEL_CPU] = new SpeedSensor("CPU Pump", PUMP_CHANNEL_CPU, state.PumpRpm, supportsControl: true);
        _temperatureSensors[PUMP_CHANNEL_CPU] = new TemperatureSensor("CPU Liquid Temp", PUMP_CHANNEL_CPU, state.LiquidTempCelsius);

        if (_isGpuUsingPump)
        {
            _speedSensors[PUMP_CHANNEL_GPU] = new SpeedSensor("GPU Pump", PUMP_CHANNEL_GPU, state.GpuPumpRpm, supportsControl: false);
            _temperatureSensors[PUMP_CHANNEL_GPU] = new TemperatureSensor("GPU Liquid Temp", PUMP_CHANNEL_GPU, state.GpuLiquidTempCelsius);
        }
    }

    protected override void RefreshImpl()
    {
        OneComputerDeviceState state;
        using (_guardManager.AwaitExclusiveAccess())
        {
            TryResetEnableDirectLighting();

            state = ReadState();
            WriteCooling();
        }

        RefreshSensors(state);
    }

    private void RefreshSensors(OneComputerDeviceState state)
    {
        _speedSensors[FAN_CHANNEL].Rpm = state.FanRpm;
        _speedSensors[PUMP_CHANNEL_CPU].Rpm = state.PumpRpm;
        _temperatureSensors[PUMP_CHANNEL_CPU].TemperatureCelsius = state.LiquidTempCelsius;

        if (_isGpuUsingPump)
        {
            _speedSensors[PUMP_CHANNEL_GPU].Rpm = state.GpuPumpRpm;
            _temperatureSensors[PUMP_CHANNEL_GPU].TemperatureCelsius = state.GpuLiquidTempCelsius;
        }
    }

    private OneComputerDeviceState ReadState()
    {
        var stateResponse = SendCommand(Commands.IncomingState, CreateStateRequestData());
        var state = ParseState(stateResponse);
        _sequenceCounter.Set(state.SequenceNumber);
        return state;
    }

    internal new OneComputerDeviceState ParseState(ReadOnlySpan<byte> stateResponse)
    {
        var baseState = base.ParseState(stateResponse);
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

        var gpuTempRaw = (double)BinaryPrimitives.ReadInt16LittleEndian(stateResponse.Slice(49, 2));
        state.GpuLiquidTempCelsius = (int)(gpuTempRaw / 25.6 + 0.5) / 10f;
        state.GpuPumpRpm = BinaryPrimitives.ReadInt16LittleEndian(stateResponse.Slice(22, 2));

        return state;
    }

    private static bool IsGpuUsingPump(OneComputerDeviceState state)
    {
        bool isTempFail = state.Status == DeviceStatus.TempFail;
        bool isGpuPumpSpeedZero = state.GpuPumpRpm == 0;
        return !isTempFail && !isGpuPumpSpeedZero;
    }
}
