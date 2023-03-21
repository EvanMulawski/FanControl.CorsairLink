using System.Buffers.Binary;

namespace CorsairLink;

public sealed class CommanderProDevice : IDevice
{
    private static class Commands
    {
        public static readonly byte ReadFirmwareVersion = 0x02;
        public static readonly byte ReadTemperatureMask = 0x10;
        public static readonly byte ReadTemperatureValue = 0x11;
        public static readonly byte ReadFanMask = 0x20;
        public static readonly byte ReadFanSpeed = 0x21;
        public static readonly byte ReadFanPower = 0x22;
        public static readonly byte WriteFanPower = 0x23;
    }

    private const int REQUEST_LENGTH = 64;
    private const int RESPONSE_LENGTH = 17;
    private const int SPEED_CHANNEL_COUNT = 6;
    private const int TEMP_CHANNEL_COUNT = 4;
    private const int DEFAULT_SPEED_CHANNEL_POWER = 50;
    private const byte PERCENT_MIN = 0x00;
    private const byte PERCENT_MAX = 0x64;

    private readonly IHidDeviceProxy _device;
    private readonly IDeviceGuardManager _guardManager;
    private readonly ILogger? _logger;
    private readonly SpeedChannelPowerTrackingStore _requestedChannelPower = new();
    private readonly Dictionary<int, SpeedSensor> _speedSensors = new();
    private readonly Dictionary<int, TemperatureSensor> _temperatureSensors = new();

    public CommanderProDevice(IHidDeviceProxy device, IDeviceGuardManager guardManager, ILogger? logger)
    {
        _device = device;
        _guardManager = guardManager;
        _logger = logger;

        var deviceInfo = device.GetDeviceInfo();
        Name = $"{deviceInfo.ProductName} ({deviceInfo.SerialNumber})";
        UniqueId = deviceInfo.DevicePath;
    }

    public string UniqueId { get; }

    public string Name { get; }

    public IReadOnlyCollection<SpeedSensor> SpeedSensors => _speedSensors.Values;

    public IReadOnlyCollection<TemperatureSensor> TemperatureSensors => _temperatureSensors.Values;

    private void Log(string message)
    {
        _logger?.Log($"{Name}: {message}");
    }

    public bool Connect()
    {
        Disconnect();

        var (opened, exception) = _device.Open();
        if (opened)
        {
            Initialize();
            return true;
        }

        if (exception is not null)
        {
            Log(exception.ToString());
        }

        return false;
    }

    public void Disconnect()
    {
        _device.Close();
    }

    public string GetFirmwareVersion()
    {
        var request = CreateRequest(Commands.ReadFirmwareVersion);
        var response = WriteAndRead(request);

        var v1 = (int)response[2];
        var v2 = (int)response[3];
        var v3 = (int)response[4];

        return $"{v1}.{v2}.{v3}";
    }

    private void Initialize()
    {
        InitializeRequestedChannelPower();
        Refresh();
    }

    public void Refresh()
    {
        WriteRequestedSpeeds();
        RefreshTemperatures();
        RefreshSpeeds();
    }

    public void SetChannelPower(int channel, int percent)
    {
        _requestedChannelPower[channel] = (byte)Utils.Clamp(percent, PERCENT_MIN, PERCENT_MAX);
    }

    private void InitializeRequestedChannelPower()
    {
        _requestedChannelPower.Clear();

        for (int i = 0; i < SPEED_CHANNEL_COUNT; i++)
        {
            _requestedChannelPower[i] = DEFAULT_SPEED_CHANNEL_POWER;
        }
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

    private int GetFanRpm(int channelId)
    {
        var request = CreateRequest(Commands.ReadFanSpeed);
        request[2] = Convert.ToByte(Utils.Clamp(channelId, 0, SPEED_CHANNEL_COUNT - 1));
        var response = WriteAndRead(request);

        return BinaryPrimitives.ReadInt16BigEndian(response.AsSpan().Slice(2));
    }

    private void SetFanPower(int channelId, byte percent)
    {
        var request = CreateRequest(Commands.WriteFanPower);
        request[2] = Convert.ToByte(Utils.Clamp(channelId, 0, SPEED_CHANNEL_COUNT - 1));
        request[3] = (byte)Utils.Clamp(percent, PERCENT_MIN, PERCENT_MAX);
        _ = WriteAndRead(request);
    }

    private void WriteRequestedSpeeds()
    {
        if (!_requestedChannelPower.Dirty)
        {
            return;
        }

        foreach (var c in _requestedChannelPower.Channels)
        {
            SetFanPower(c, _requestedChannelPower[c]);
        }

        _requestedChannelPower.ResetDirty();
    }

    private float GetTemperatureSensorValue(int channelId)
    {
        var request = CreateRequest(Commands.ReadTemperatureValue);
        request[2] = Convert.ToByte(Utils.Clamp(channelId, 0, TEMP_CHANNEL_COUNT - 1));
        var response = WriteAndRead(request);

        return BinaryPrimitives.ReadInt16BigEndian(response.AsSpan().Slice(2)) / 100f;
    }

    private IReadOnlyCollection<SpeedSensor> GetSpeedSensors()
    {
        var request = CreateRequest(Commands.ReadFanMask);
        var response = WriteAndRead(request);

        var sensors = new List<SpeedSensor>();

        for (int ch = 0, i = 2; ch < SPEED_CHANNEL_COUNT; ch++, i++)
        {
            int? rpm = default;
            var connected = response[i] > 0x00;

            if (connected)
            {
                rpm = GetFanRpm(ch);
            }

            sensors.Add(new SpeedSensor($"Fan #{ch + 1}", ch, rpm, supportsControl: true));
        }

        return sensors;
    }

    private IReadOnlyCollection<TemperatureSensor> GetTemperatureSensors()
    {
        var request = CreateRequest(Commands.ReadTemperatureMask);
        var response = WriteAndRead(request);

        var sensors = new List<TemperatureSensor>();

        for (int ch = 0, i = 2; ch < TEMP_CHANNEL_COUNT; ch++, i++)
        {
            float? temp = default;
            var connected = response[i] == 0x01;

            if (connected)
            {
                temp = GetTemperatureSensorValue(ch);
            }

            sensors.Add(new TemperatureSensor($"Temp #{ch + 1}", ch, temp));
        }

        return sensors;
    }

    private byte[] WriteAndRead(byte[] buffer)
    {
        var response = CreateResponse();

        using (_guardManager.AwaitExclusiveAccess())
        {
            Write(buffer);
            Read(response);
        }

        return response;
    }

    private void Write(byte[] buffer)
    {
        _device.Write(buffer);
    }

    private void Read(byte[] buffer)
    {
        _device.Read(buffer);
    }

    private static byte[] CreateRequest(byte command)
    {
        var writeBuf = new byte[REQUEST_LENGTH];
        writeBuf[1] = command;
        return writeBuf;
    }

    private static byte[] CreateResponse()
    {
        return new byte[RESPONSE_LENGTH];
    }
}
