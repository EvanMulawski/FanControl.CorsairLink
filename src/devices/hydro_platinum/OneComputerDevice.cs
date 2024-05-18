using CorsairLink.Devices.HydroPlatinum;

namespace CorsairLink.Devices;

public class OneComputerDevice : HydroPlatinumDevice
{
    private const int PUMP_CHANNEL_CPU = -1;
    private const int PUMP_CHANNEL_GPU = -2;
    private const int FAN_CHANNEL = 0;

    private bool _isGpuUsingPump;

    private readonly OneComputerDataReader _dataReader = new();
    private readonly OneComputerDataWriter _dataWriter = new();

    public OneComputerDevice(IHidDeviceProxy device, IDeviceGuardManager guardManager, OneComputerDeviceOptions options, ILogger logger)
        : base(device, guardManager, options, logger)
    {

    }

    protected override void Initialize()
    {
        OneComputerDeviceState state;
        using (_guardManager.AwaitExclusiveAccess())
        {
            state = ReadState();
            ResetEnableDirectLighting();
        }

        _isGpuUsingPump = state.IsGpuUsingPump();

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
        var data = _dataWriter.CreateIncomingStateCommandData();
        var stateResponse = SendCommand(Commands.IncomingState, data);
        var state = (OneComputerDeviceState)_dataReader.GetState(stateResponse);
        _sequenceCounter.Set(state.SequenceNumber);
        return state;
    }
}
