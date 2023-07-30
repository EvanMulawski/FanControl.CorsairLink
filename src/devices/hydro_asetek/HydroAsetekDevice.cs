using CorsairLink.Asetek;
using System.Buffers.Binary;
using System.Text;

namespace CorsairLink.Devices;

public sealed class HydroAsetekDevice : DeviceBase
{
    internal static class Commands
    {
        public static readonly byte SetConfiguration = 0x10;
        public static readonly byte SetFanCurve = 0x11;
        public static readonly byte SetPumpPower = 0x13;
    }

    // exact model name is not stored on-device
    // device returns "Corsair Hydro Series 7289 USB Device"
    internal static readonly IReadOnlyDictionary<int, string> ModelNames = new Dictionary<int, string>
    {
        { 0x0c02, "Hydro H80i GT" },
        { 0x0c03, "Hydro H100i GTX" },
        { 0x0c07, "Hydro H110i GTX" },
        { 0x0c08, "Hydro H80i GT V2" },
        { 0x0c09, "Hydro H100i GT V2" },
        { 0x0c0a, "Hydro H110i GT V2" },
    };

    private const int DEFAULT_SPEED_CHANNEL_POWER = 50;
    private const byte PERCENT_MIN = 0;
    private const byte PERCENT_MAX = 100;
    private const int PUMP_CHANNEL = -1;
    private const int FAN_CHANNEL = 0;

    private readonly IAsetekDeviceProxy _device;
    private readonly IDeviceGuardManager _guardManager;
    private readonly ChannelTrackingStore _requestedChannelPower = new();
    private readonly Dictionary<int, SpeedSensor> _speedSensors = new();
    private readonly Dictionary<int, TemperatureSensor> _temperatureSensors = new();

    private string _firmwareVersion = string.Empty;

    public HydroAsetekDevice(IAsetekDeviceProxy device, IDeviceGuardManager guardManager, ILogger logger)
        : base(logger)
    {
        _device = device;
        _guardManager = guardManager;

        var deviceInfo = device.GetDeviceInfo();
        UniqueId = deviceInfo.DevicePath;
        Name = $"{ModelNames[deviceInfo.ProductId]} ({Utils.ToMD5HexString(UniqueId)})";
    }

    public override string UniqueId { get; }

    public override string Name { get; }

    public override IReadOnlyCollection<SpeedSensor> SpeedSensors => _speedSensors.Values;

    public override IReadOnlyCollection<TemperatureSensor> TemperatureSensors => _temperatureSensors.Values;

    public override bool Connect()
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

        _device.Close();
    }

    public override string GetFirmwareVersion()
    {
        return _firmwareVersion;
    }

    private void Initialize()
    {
        var state = SetFanTypeToPwm();
        _firmwareVersion = $"{state.FirmwareVersionMajor}.{state.FirmwareVersionMinor}.{state.FirmwareVersionRevision1}.{state.FirmwareVersionRevision2}";

        InitializeSpeedChannelStores();
        Refresh();
    }

    public override void Refresh()
    {
        var state = WriteRequestedSpeeds();
        _speedSensors[PUMP_CHANNEL].Rpm = state.PumpRpm;
        _speedSensors[FAN_CHANNEL].Rpm = state.FanRpm;
        _temperatureSensors[PUMP_CHANNEL].TemperatureCelsius = state.LiquidTempCelsius;

        if (CanLogDebug)
        {
            LogDebug(GetStateStringRepresentation());
        }
    }

    public override void SetChannelPower(int channel, int percent)
    {
        LogDebug($"SetChannelPower {channel} {percent}%");

        _requestedChannelPower[channel] = (byte)Utils.Clamp(percent, PERCENT_MIN, PERCENT_MAX);
    }

    private void InitializeSpeedChannelStores()
    {
        LogDebug("InitializeSpeedChannelStores");

        _requestedChannelPower.Clear();
        SetChannelPower(PUMP_CHANNEL, DEFAULT_SPEED_CHANNEL_POWER);
        _speedSensors[PUMP_CHANNEL] = new SpeedSensor("Pump", PUMP_CHANNEL, default, supportsControl: true);
        SetChannelPower(FAN_CHANNEL, DEFAULT_SPEED_CHANNEL_POWER);
        _speedSensors[FAN_CHANNEL] = new SpeedSensor("Fan", FAN_CHANNEL, default, supportsControl: true);
        _temperatureSensors[PUMP_CHANNEL] = new TemperatureSensor("Liquid Temp", PUMP_CHANNEL, default);
    }

    private State SetFanPower(byte percent)
    {
        LogDebug($"SetFanPower {percent}%");

        var requestData = new byte[13];
        requestData[1] = 0x00; // 0C (min temp)
        requestData[2] = 0x64; // 100C (max temp)
        requestData[7] = percent;
        requestData[8] = percent;
        var response = WriteAndRead(CreateRequest(Commands.SetFanCurve, requestData));
        response.ThrowIfError();
        return response.GetState();
    }

    private State SetPumpPower(byte percent)
    {
        LogDebug($"SetPumpPower {percent}%");

        var requestData = new byte[1] { percent };
        var response = WriteAndRead(CreateRequest(Commands.SetPumpPower, requestData));
        response.ThrowIfError();
        return response.GetState();
    }

    private State SetFanTypeToPwm()
    {
        LogDebug("SetFanTypeToPwm");

        var requestData = new byte[18];
        requestData[17] = 0x01; // PWM
        var response = WriteAndRead(CreateRequest(Commands.SetConfiguration, requestData));
        response.ThrowIfError();
        return response.GetState();
    }

    private State WriteRequestedSpeeds()
    {
        LogDebug("WriteRequestedSpeeds");

        if (_requestedChannelPower.ApplyChanges())
        {
            _ = SetPumpPower(_requestedChannelPower[PUMP_CHANNEL]);
        }

        // have to write to read anyway
        return SetFanPower(_requestedChannelPower[FAN_CHANNEL]);
    }

    private string GetStateStringRepresentation()
    {
        var sb = new StringBuilder().AppendLine("STATE");

        foreach (var channel in _requestedChannelPower.Channels)
        {
            sb.AppendLine($"Requested power for channel {channel}: {_requestedChannelPower[channel]} %");
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

    private DeviceResponse WriteAndRead(byte[] buffer)
    {
        DeviceResponse response;

        if (CanLogDebug)
        {
            LogDebug($"WRITE: {buffer.ToHexString()}");
        }

        using (_guardManager.AwaitExclusiveAccess())
        {
            var data = _device.WriteAndRead(buffer);

            if (CanLogDebug)
            {
                LogDebug($"READ:  {data.ToHexString()}");
            }

            response = new DeviceResponse(buffer, data);
        }

        return response;
    }

    private byte[] CreateRequest(byte command, ReadOnlySpan<byte> data = default)
    {
        var buffer = new byte[data.Length + 1];
        buffer[0] = command;
        data.CopyTo(buffer.AsSpan(1));
        return buffer;
    }

    internal sealed class DeviceResponse
    {
        public DeviceResponse(byte[] request, byte[] response)
        {
            Request = request;
            Response = response;
            IsError = !IsValid();
        }

        public byte[] Request { get; }
        private byte[] Response { get; }
        public bool IsError { get; }

        public State GetState()
        {
            // [0..1] fan rpm
            // [8..9] pump rpm
            // [10] liquid temp whole part
            // [14] liquid temp fractional part
            // [23..26] firmware version

            return new State
            {
                FirmwareVersionMajor = Response[23],
                FirmwareVersionMinor = Response[24],
                FirmwareVersionRevision1 = Response[25],
                FirmwareVersionRevision2 = Response[26],
                FanRpm = BinaryPrimitives.ReadUInt16BigEndian(Response.AsSpan(0, 2)),
                PumpRpm = BinaryPrimitives.ReadUInt16BigEndian(Response.AsSpan(8, 2)),
                LiquidTempCelsius = Response[10] + Response[14] * 0.1f,
            };
        }

        public void ThrowIfError()
        {
            if (IsError)
            {
                throw CreateException("Response was invalid.", Request, Response);
            }
        }

        public void Throw(string message)
        {
            throw CreateException(message, Request, Response);
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
            return Response[11] == Request[0];
        }
    }

    internal sealed class State
    {
        public int FirmwareVersionMajor { get; set; }
        public int FirmwareVersionMinor { get; set; }
        public int FirmwareVersionRevision1 { get; set; }
        public int FirmwareVersionRevision2 { get; set; }
        public int FanRpm { get; set; }
        public int PumpRpm { get; set; }
        public float LiquidTempCelsius { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("fanRpm={0}, ", FanRpm);
            sb.AppendFormat("pumpRpm={0}, ", PumpRpm);
            sb.AppendFormat("liquidTempCelsius={0}", LiquidTempCelsius);
            return sb.ToString();
        }
    }
}
