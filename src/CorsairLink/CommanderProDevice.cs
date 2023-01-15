using HidSharp;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

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

    private readonly HidDevice _device;
    private readonly ILogger? _logger;
    private HidStream? _stream;
    private readonly SpeedChannelPowerTrackingStore _requestedChannelPower = new();
    private readonly Dictionary<int, SpeedSensor> _speedSensors = new();
    private readonly Dictionary<int, TemperatureSensor> _temperatureSensors = new();

    public CommanderProDevice(HidDevice device, ILogger? logger)
    {
        _device = device;
        _logger = logger;
        Name = $"{device.GetProductName()} ({device.GetSerialNumber()})";
    }

    public string UniqueId => _device.DevicePath;

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

        if (_device.TryOpen(out _stream))
        {
            Initialize();
            return true;
        }

        return false;
    }

    public void Disconnect()
    {
        _stream?.Dispose();
        _stream = null;
    }

    private void ThrowIfNotConnected()
    {
        if (_stream is null)
        {
            throw new InvalidOperationException("Not connected!");
        }
    }

    public string GetFirmwareVersion()
    {
        ThrowIfNotConnected();

        var request = CreateRequest(Commands.ReadFirmwareVersion, REQUEST_LENGTH);
        Write(_stream, request);
        var response = CreateResponse(RESPONSE_LENGTH);
        Read(_stream, response);

        var v1 = (int)response[2];
        var v2 = (int)response[3];
        var v3 = (int)response[4];

        return $"{v1}.{v2}.{v3}";
    }

    private void Initialize()
    {
        InitializeRequestedChannelPower();
        RefreshTemperatures();
        RefreshSpeeds();
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
        ThrowIfNotConnected();

        var request = CreateRequest(Commands.ReadFanSpeed, REQUEST_LENGTH);
        request[2] = Convert.ToByte(Utils.Clamp(channelId, 0, SPEED_CHANNEL_COUNT - 1));
        Write(_stream, request);
        var response = CreateResponse(RESPONSE_LENGTH);
        Read(_stream, response);

        return BinaryPrimitives.ReadInt16BigEndian(response.AsSpan().Slice(2));
    }

    private void SetFanPower(int channelId, byte percent)
    {
        ThrowIfNotConnected();

        var request = CreateRequest(Commands.WriteFanPower, REQUEST_LENGTH);
        request[2] = Convert.ToByte(Utils.Clamp(channelId, 0, SPEED_CHANNEL_COUNT - 1));
        request[3] = (byte)Utils.Clamp(percent, PERCENT_MIN, PERCENT_MAX);
        Write(_stream, request);
        var response = CreateResponse(RESPONSE_LENGTH);
        Read(_stream, response);
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

    private int GetTemperatureSensorValue(int channelId)
    {
        ThrowIfNotConnected();

        var request = CreateRequest(Commands.ReadTemperatureValue, REQUEST_LENGTH);
        request[2] = Convert.ToByte(Utils.Clamp(channelId, 0, TEMP_CHANNEL_COUNT - 1));
        Write(_stream, request);
        var response = CreateResponse(RESPONSE_LENGTH);
        Read(_stream, response);

        return BinaryPrimitives.ReadInt16BigEndian(response.AsSpan().Slice(2)) / 100;
    }

    private IReadOnlyCollection<SpeedSensor> GetSpeedSensors()
    {
        ThrowIfNotConnected();

        var request = CreateRequest(Commands.ReadFanMask, REQUEST_LENGTH);
        Write(_stream, request);
        var response = CreateResponse(RESPONSE_LENGTH);
        Read(_stream, response);

        var sensors = new List<SpeedSensor>();

        for (int ch = 0, i = 2; ch < SPEED_CHANNEL_COUNT; ch++, i++)
        {
            int? rpm = default;
            var connected = response[i] > 0x00;

            if (connected)
            {
                rpm = GetFanRpm(ch);
            }

            sensors.Add(new SpeedSensor($"Fan #{ch + 1}", ch, rpm));
        }

        return sensors;
    }

    private IReadOnlyCollection<TemperatureSensor> GetTemperatureSensors()
    {
        ThrowIfNotConnected();

        var request = CreateRequest(Commands.ReadTemperatureMask, REQUEST_LENGTH);
        Write(_stream, request);
        var response = CreateResponse(RESPONSE_LENGTH);
        Read(_stream, response);

        var sensors = new List<TemperatureSensor>();

        for (int ch = 0, i = 2; ch < TEMP_CHANNEL_COUNT; ch++, i++)
        {
            int? temp = default;
            var connected = response[i] == 0x01;

            if (connected)
            {
                temp = GetTemperatureSensorValue(ch);
            }

            sensors.Add(new TemperatureSensor($"Temp #{ch + 1}", ch, temp));
        }

        return sensors;
    }

    private static void Write(Stream? stream, byte[] buffer)
    {
        try
        {
            stream?.Write(buffer, 0, buffer.Length);
        }
        catch (ObjectDisposedException)
        {
            // disconnected, ignore
        }
    }

    private static void Read(Stream? stream, byte[] buffer)
    {
        try
        {
            stream?.Read(buffer, 0, buffer.Length);
        }
        catch (ObjectDisposedException)
        {
            // disconnected, ignore
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte[] CreateRequest(byte command, int length)
    {
        var writeBuf = new byte[length];
        writeBuf[1] = command;
        return writeBuf;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte[] CreateResponse(int length)
    {
        return new byte[length];
    }
}
