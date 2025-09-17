using System.Text;

namespace CorsairLink.Devices;

public sealed class HidPsuDevice : DeviceBase
{
    private static class CommandModes
    {
        public static readonly byte Read = 0x03;
        public static readonly byte Write = 0x02;
    }

    private static class FanControlModes
    {
        public static readonly byte Normal = 0x00;
        public static readonly byte Manual = 0x01;
    }

    private static class Commands
    {
        public static readonly byte ReadFirmwareVersion = 0xd4;
        public static readonly byte Handshake = 0xfe;
        public static readonly byte WriteFanControlMode = 0xf0;
        public static readonly byte ReadTemperature1 = 0x8d;
        public static readonly byte ReadTemperature2 = 0x8e;
        public static readonly byte ReadFanSpeed = 0x90;
        public static readonly byte WriteFanPower = 0x3b;
    }

    private const int REQUEST_LENGTH = 65;
    private const int RESPONSE_LENGTH = 64;
    private const int TEMP_CHANNEL_COUNT = 2;
    private const int DEFAULT_SPEED_CHANNEL_POWER = 50;
    private const int SPEED_CHANNEL = 0;
    private const int PERCENT_MIN = 0;
    private const int PERCENT_MAX = 100;
    private const int DEFAULT_ZERO_RPM_DUTY_THRESHOLD = 30;

    private readonly IHidDeviceProxy _device;
    private readonly IDeviceGuardManager _guardManager;
    private readonly int _zeroRpmDutyThreshold;
    private readonly ChannelTrackingStore _requestedChannelPower = new();
    private readonly ChannelTrackingStore _fanControlModeStore = new();
    private readonly Dictionary<int, SpeedSensor> _speedSensors = new();
    private readonly Dictionary<int, TemperatureSensor> _temperatureSensors = new();

    private string _name;
    private readonly string _serialNumber;

    public HidPsuDevice(IHidDeviceProxy device, IDeviceGuardManager guardManager, HidPsuDeviceOptions options, ILogger logger)
        : base(logger)
    {
        _device = device;
        _guardManager = guardManager;
        _zeroRpmDutyThreshold = GetZeroRpmDutyThreshold(options.ZeroRpmDutyThreshold);

        var deviceInfo = device.GetDeviceInfo();
        _serialNumber = deviceInfo.SerialNumber;
        _name = "Corsair PSU";

        UniqueId = deviceInfo.DevicePath;
    }

    private string GetName() => $"{_name} ({_serialNumber})";

    public override string UniqueId { get; }

    public override string Name => GetName();

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
        var request = CreateRequest(CommandModes.Read, Commands.ReadFirmwareVersion);
        var response = WriteAndRead(request);

        if (response.IsError)
        {
            return "UNKNOWN";
        }

        var data = response.GetData();
        var v1 = (int)data[0];
        var v2 = (int)data[1];
        var v3 = (int)data[2];
        var v4 = (int)data[3];

        return $"{v1}.{v2}.{v3}.{v4}";
    }

    private void Initialize()
    {
        UpdateDeviceName();
        InitializeSpeedChannelStores();
        Refresh();
    }

    private void UpdateDeviceName()
    {
        using (_guardManager.AwaitExclusiveAccess())
        {
            var response = PerformHandshake();
            response.ThrowIfError();

            var modelNameData = response.GetData();
            _name = Utils.ParseString(modelNameData.ToArray());
        }
    }

    public override void Refresh(CancellationToken cancellationToken = default)
    {
        WriteRequestedSpeeds();
        RefreshTemperatures();
        RefreshSpeeds();

        if (CanLogDebug)
        {
            LogDebugState();
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
        var request = CreateRequest(CommandModes.Read, Commands.ReadFanSpeed);
        var response = WriteAndRead(request);

        if (response.IsError)
        {
            return default;
        }

        return (int)Utils.FromLinear11(response.GetData());
    }

    private void SetFanPower(byte percent)
    {
        LogDebug($"SetFanPower {percent}%");
        var request = CreateRequest(CommandModes.Write, Commands.WriteFanPower, percent);
        _ = WriteAndRead(request);
    }

    private void SetFanControlMode(byte mode)
    {
        LogDebug($"SetFanControlMode {mode}");
        var request = CreateRequest(CommandModes.Write, Commands.WriteFanControlMode, mode);
        _ = WriteAndRead(request);
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
        var request = CreateRequest(CommandModes.Read, channelId == 0 ? Commands.ReadTemperature1 : Commands.ReadTemperature2);
        var response = WriteAndRead(request);

        if (response.IsError)
        {
            return default;
        }

        return Utils.FromLinear11(response.GetData());
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

    private DeviceResponse WriteAndRead(byte[] buffer)
    {
        var response = CreateResponse();

        using (_guardManager.AwaitExclusiveAccess())
        {
            PerformHandshake();

            Write(buffer);
            Read(response);
        }

        return new DeviceResponse(buffer, response);
    }

    private DeviceResponse PerformHandshake()
    {
        var request = CreateHandshakeRequest();
        var response = CreateResponse();

        Write(request);
        Read(response);

        return new DeviceResponse(request, response);
    }

    private void Write(byte[] buffer)
    {
        if (CanLogDebug)
        {
            LogDebug($"WRITE: {buffer.ToHexString()}");
        }

        _device.Write(buffer);
    }

    private void Read(byte[] buffer)
    {
        _device.Read(buffer);

        if (CanLogDebug)
        {
            LogDebug($"READ:  {buffer.ToHexString()}");
        }
    }

    private static byte[] CreateRequest(byte commandMode, byte command, byte data = default)
    {
        // [0] report id (always zero)
        // [1] command mode (read/write)
        // [2] command
        // [3..] data

        var writeBuf = new byte[REQUEST_LENGTH];
        writeBuf[1] = commandMode;
        writeBuf[2] = command;
        writeBuf[3] = data;
        return writeBuf;
    }

    private static byte[] CreateHandshakeRequest()
    {
        // handshake request swaps positions of command and command mode
        return CreateRequest(Commands.Handshake, CommandModes.Read);
    }

    private static byte[] CreateResponse()
    {
        return new byte[RESPONSE_LENGTH];
    }

    private void LogDebugState()
    {
        var sb = new StringBuilder();
        sb.AppendLine("STATE:");

        foreach (var k in _speedSensors.Keys)
        {
            sb.AppendLine(_speedSensors[k].ToString());
        }
        foreach (var k in _temperatureSensors.Keys)
        {
            sb.AppendLine(_temperatureSensors[k].ToString());
        }

        LogDebug(sb.ToString());
    }

    private static int GetZeroRpmDutyThreshold(int? userProvidedZeroRpmDutyThreshold)
    {
        if (userProvidedZeroRpmDutyThreshold.HasValue)
        {
            return Utils.Clamp(userProvidedZeroRpmDutyThreshold.Value, 1, 99);
        }

        return DEFAULT_ZERO_RPM_DUTY_THRESHOLD;
    }

    internal sealed class DeviceResponse
    {
        private const byte RESPONSE_ERROR_CODE = 0xfe;

        public DeviceResponse(byte[] request, byte[] response)
        {
            Request = request;
            Response = response;
            IsError = !IsValid();
        }

        public byte[] Request { get; }
        private byte[] Response { get; }
        public bool IsError { get; }

        public ReadOnlySpan<byte> GetData() => Response.AsSpan(3);

        public void ThrowIfError()
        {
            if (IsError)
            {
                throw CreateException("Response was invalid.", Request, Response);
            }
        }

        private static CorsairLinkDeviceException CreateException(string message, ReadOnlySpan<byte> request, ReadOnlySpan<byte> response)
        {
            var exception = new CorsairLinkDeviceException(message);
            exception.Data[nameof(request)] = request.ToHexString();
            exception.Data[nameof(response)] = response.ToHexString();
            return exception;
        }

        public bool IsValid()
        {
            return Response[1] switch
            {
                RESPONSE_ERROR_CODE when Response[3] == RESPONSE_ERROR_CODE => false,
                _ when Response[2] == RESPONSE_ERROR_CODE => false,
                _ when Request[1] == Response[1] && Request[2] == Response[2] => true,
                _ => false
            };
        }
    }
}
