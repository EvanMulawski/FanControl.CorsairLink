using CorsairLink.FlexUsb;
using System.Text;

namespace CorsairLink.Devices;

public sealed class FlexUsbPsuDevice : DeviceBase
{
    private static class FanControlModes
    {
        public static readonly byte Normal = 0x00;
        public static readonly byte Manual = 0x01;
    }

    private const int TEMP_CHANNEL_COUNT = 3;
    private const int DEFAULT_SPEED_CHANNEL_POWER = 50;
    private const int SPEED_CHANNEL = 0;
    private const int PERCENT_MIN = 0;
    private const int PERCENT_MAX = 100;
    private const int DEFAULT_ZERO_RPM_DUTY_THRESHOLD = 15; // 15% is a safe minimum for the NR140P fan in this product

    private readonly IFlexUsbDeviceProxy _device;
    private readonly FlexUsbDeviceInfo _deviceInfo;
    private readonly IDeviceGuardManager _guardManager;
    private readonly int _zeroRpmDutyThreshold;
    private readonly ChannelTrackingStore _requestedChannelPower = new();
    private readonly ChannelTrackingStore _fanControlModeStore = new();
    private readonly Dictionary<int, SpeedSensor> _speedSensors = new();
    private readonly Dictionary<int, TemperatureSensor> _temperatureSensors = new();

    public FlexUsbPsuDevice(IFlexUsbDeviceProxy device, IDeviceGuardManager guardManager, FlexUsbPsuDeviceOptions options, ILogger logger)
        : base(logger)
    {
        _device = device;
        _deviceInfo = device.GetDeviceInfo();
        _guardManager = guardManager;
        _zeroRpmDutyThreshold = GetZeroRpmDutyThreshold(options.ZeroRpmDutyThreshold);

        UniqueId = _deviceInfo.DevicePath;
        Name = $"{_deviceInfo.ProductName} ({Utils.ToMD5HexString(UniqueId)})";
    }

    public override string UniqueId { get; }

    public override string Name { get; }

    public override IReadOnlyCollection<SpeedSensor> SpeedSensors => _speedSensors.Values;

    public override IReadOnlyCollection<TemperatureSensor> TemperatureSensors => _temperatureSensors.Values;

    public override bool Connect(CancellationToken cancellationToken = default)
    {
        LogDebug("Connect");

        Disconnect();

        var (opened, exception) = _device.Open();
        if (opened)
        {
            Initialize();
            return true;
        }

        if (exception is not null)
        {
            LogError(exception);
        }

        return false;
    }

    public override void Disconnect()
    {
        LogDebug("Disconnect");

        try
        {
            SetFanControlMode(FanControlModes.Normal);
        }
        catch
        {
            // ignore
        }

        _device.Close();
    }

    public override string GetFirmwareVersion()
    {
        using (_guardManager.AwaitExclusiveAccess())
        {
            var data = _device.Read(CommandCode.READ_MFR_REVISION, 3);
            return $"{data[0]}.{data[1]}.{data[2]}";
        }
    }

    private void Initialize()
    {
        InitializeSpeedChannelStores();
        Refresh();
    }

    public override void Refresh(CancellationToken cancellationToken = default)
    {
        WriteRequestedSpeeds();
        RefreshTemperatures();
        RefreshSpeeds();

        if (CanLogDebug)
        {
            LogDebug(GetStateStringRepresentation());
        }
    }

    public override void SetChannelPower(int channel, int percent)
    {
        var zeroRpmRequested = percent < _zeroRpmDutyThreshold;
        LogDebug($"SetChannelPower {channel} {percent}% (isZeroRpm={zeroRpmRequested})");

        _requestedChannelPower[SPEED_CHANNEL] = (byte)Utils.Clamp(percent, PERCENT_MIN, PERCENT_MAX);

        // When the user sets the power to a value below the zero-RPM duty threshold, set the fan control mode to Normal.
        // This mode allows the device to control the fan speed and allows for zero-RPM operation.
        // Set the fan control mode to Manual if the user sets the power at or above the threshold.
        _fanControlModeStore[SPEED_CHANNEL] = zeroRpmRequested ? FanControlModes.Normal : FanControlModes.Manual;
    }

    private void InitializeSpeedChannelStores()
    {
        LogDebug("InitializeSpeedChannelStores");
        _requestedChannelPower[SPEED_CHANNEL] = DEFAULT_SPEED_CHANNEL_POWER;
        _fanControlModeStore[SPEED_CHANNEL] = FanControlModes.Manual;
    }

    private void RefreshTemperatures()
    {
        var sensors = GetTemperatureSensors();

        foreach (var sensor in sensors)
        {
            if (!_temperatureSensors.TryGetValue(sensor.Channel, out var existingSensor))
            {
                _temperatureSensors[sensor.Channel] = sensor;
                continue;
            }

            existingSensor.TemperatureCelsius = sensor.TemperatureCelsius;
        }
    }

    private void RefreshSpeeds()
    {
        var sensors = GetSpeedSensors();

        foreach (var sensor in sensors)
        {
            if (!_speedSensors.TryGetValue(sensor.Channel, out var existingSensor))
            {
                _speedSensors[sensor.Channel] = sensor;
                continue;
            }

            existingSensor.Rpm = sensor.Rpm;
        }
    }

    private int? GetFanRpm()
    {
        using (_guardManager.AwaitExclusiveAccess())
        {
            return (int)_device.ReadNumber(CommandCode.READ_FAN_SPEED_1);
        }
    }

    private void SetFanPower(byte percent)
    {
        LogDebug($"SetFanPower {percent}%");
        using (_guardManager.AwaitExclusiveAccess())
        {
            _device.WriteByte(CommandCode.FAN_COMMAND_1, percent);
        }
    }

    private void SetFanControlMode(byte mode)
    {
        LogDebug($"SetFanControlMode {mode}");
        using (_guardManager.AwaitExclusiveAccess())
        {
            _device.WriteByte(CommandCode.FAN_INDEX, mode);
        }
    }

    private void WriteRequestedSpeeds()
    {
        LogDebug("WriteRequestedSpeeds");

        if (_requestedChannelPower.ApplyChanges())
        {
            SetFanPower(_requestedChannelPower[SPEED_CHANNEL]);
        }

        if (_fanControlModeStore.ApplyChanges())
        {
            var mode = _fanControlModeStore[SPEED_CHANNEL];

            if (CanLogDebug)
            {
                LogDebug($"Changing fan control mode ({mode:X2})");
            }

            SetFanControlMode(mode);
        }
    }

    private float? GetTemperatureSensorValue(int channelId)
    {
        var command = CommandCode.READ_TEMPERATURE_3;

        switch (channelId)
        {
            case 0:
                command = CommandCode.READ_TEMPERATURE_1;
                break;
            case 1:
                command = CommandCode.READ_TEMPERATURE_2;
                break;
            case 2:
                command = CommandCode.READ_TEMPERATURE_3;
                break;
        }

        using (_guardManager.AwaitExclusiveAccess())
        {
            return _device.ReadNumber(command);
        }
    }

    private IReadOnlyCollection<SpeedSensor> GetSpeedSensors()
    {
        var sensors = new List<SpeedSensor>();

        var rpm = GetFanRpm();
        sensors.Add(new SpeedSensor("Fan #1", SPEED_CHANNEL, rpm, supportsControl: true));

        return sensors;
    }

    private IReadOnlyCollection<TemperatureSensor> GetTemperatureSensors()
    {
        var sensors = new List<TemperatureSensor>();

        for (int ch = 0; ch < TEMP_CHANNEL_COUNT; ch++)
        {
            var temp = GetTemperatureSensorValue(ch);
            sensors.Add(new TemperatureSensor($"Temp #{ch + 1}", ch, temp));
        }

        return sensors;
    }

    private string GetStateStringRepresentation()
    {
        var sb = new StringBuilder().AppendLine("STATE");

        foreach (var channel in _requestedChannelPower.Channels)
        {
            sb.AppendLine($"Requested power for channel {channel}: {_requestedChannelPower[channel]} %");
        }

        foreach (var channel in _fanControlModeStore.Channels)
        {
            sb.AppendLine($"Requested fan control mode for channel {channel}: {_fanControlModeStore[channel]}");
        }

        foreach (var sensor in SpeedSensors)
        {
            sb.AppendLine(sensor.ToString());
        }

        foreach (var sensor in TemperatureSensors)
        {
            sb.AppendLine(sensor.ToString());
        }

        return sb.ToString();
    }

    private static int GetZeroRpmDutyThreshold(int? userProvidedZeroRpmDutyThreshold)
    {
        if (userProvidedZeroRpmDutyThreshold.HasValue)
        {
            return Utils.Clamp(userProvidedZeroRpmDutyThreshold.Value, 1, 99);
        }

        return DEFAULT_ZERO_RPM_DUTY_THRESHOLD;
    }
}
