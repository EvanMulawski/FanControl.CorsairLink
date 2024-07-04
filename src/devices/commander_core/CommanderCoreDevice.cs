using CorsairLink.Devices.CommanderCore;
using System.Text;

namespace CorsairLink.Devices;

public sealed class CommanderCoreDevice : DeviceBase
{
    private static class Commands
    {
        // This device family supports custom communication handles.
        // Claim use of the 0xfc (236) handle.
        // Note to other developers reading this: choose a different handle.
        private const byte HANDLE_ID = 0xfc;

        public static ReadOnlySpan<byte> EnterSoftwareMode => new byte[] { 0x01, 0x03, 0x00, 0x02 };
        public static ReadOnlySpan<byte> EnterHardwareMode => new byte[] { 0x01, 0x03, 0x00, 0x01 };
        public static ReadOnlySpan<byte> ReadFirmwareVersion => new byte[] { 0x02, 0x13 };
        public static ReadOnlySpan<byte> OpenEndpoint => new byte[] { 0x0d, HANDLE_ID };
        public static ReadOnlySpan<byte> CloseEndpoint => new byte[] { 0x05, 0x01, HANDLE_ID };
        public static ReadOnlySpan<byte> Read => new byte[] { 0x08, HANDLE_ID };
        public static ReadOnlySpan<byte> Write => new byte[] { 0x06, HANDLE_ID };
    }

    private static class Endpoints
    {
        public static ReadOnlySpan<byte> GetSpeeds => new byte[] { 0x17 };
        public static ReadOnlySpan<byte> GetConnectedSpeeds => new byte[] { 0x1a };
        public static ReadOnlySpan<byte> GetTemperatures => new byte[] { 0x21 };
        public static ReadOnlySpan<byte> SoftwareSpeedFixedPercent => new byte[] { 0x18 };
    }

    private static class DataTypes
    {
        public static ReadOnlySpan<byte> Speeds => new byte[] { 0x06, 0x00 };
        public static ReadOnlySpan<byte> ConnectedSpeeds => new byte[] { 0x09, 0x00 };
        public static ReadOnlySpan<byte> Temperatures => new byte[] { 0x10, 0x00 };
        public static ReadOnlySpan<byte> SoftwareSpeedFixedPercent => new byte[] { 0x07, 0x00 };
    }

    private const int DEFAULT_SPEED_CHANNEL_POWER = 50;
    private const int DEFAULT_SPEED_CHANNEL_POWER_PUMP = 100;
    private const byte PERCENT_MIN = 0;
    private const byte PERCENT_MAX = 100;
    private const int SEND_COMMAND_WAIT_FOR_DATA_TYPE_READ_TIMEOUT_MS = 500;
    private const int PUMP_CHANNEL = 0;

    private readonly IHidDeviceProxy _device;
    private readonly IDeviceGuardManager _guardManager;
    private byte _speedChannelCount;
    private bool _isChangingDeviceMode;
    private readonly bool _firstChannelExt;
    private readonly int _packetSize;
    private readonly byte _pumpPowerMinimum;
    private readonly ChannelTrackingStore _requestedChannelPower = new();
    private readonly Dictionary<int, SpeedSensor> _speedSensors = new();
    private readonly Dictionary<int, TemperatureSensor> _temperatureSensors = new();

    public CommanderCoreDevice(IHidDeviceProxy device, IDeviceGuardManager guardManager, CommanderCoreDeviceOptions options, ILogger logger)
        : base(logger)
    {
        _device = device;
        _guardManager = guardManager;

        var deviceInfo = device.GetDeviceInfo();
        Name = $"{deviceInfo.ProductName} ({deviceInfo.SerialNumber})";
        UniqueId = deviceInfo.DevicePath;

        _firstChannelExt = options.IsFirstChannelExt ?? CommanderCoreDeviceOptions.IsFirstChannelExtDefault;
        _packetSize = options.PacketSize ?? CommanderCoreDeviceOptions.PacketSizeDefault;
        _pumpPowerMinimum = (byte)Utils.Clamp(options.MinimumPumpPower ?? CommanderCoreDeviceOptions.MinimumPumpPowerDefault, PERCENT_MIN, PERCENT_MAX);
    }

    public override string UniqueId { get; }

    public override string Name { get; }

    public override IReadOnlyCollection<SpeedSensor> SpeedSensors => _speedSensors.Values;

    public override IReadOnlyCollection<TemperatureSensor> TemperatureSensors => _temperatureSensors.Values;

    public override bool Connect(CancellationToken cancellationToken = default)
    {
        Disconnect();

        var (opened, exception) = _device.Open();
        if (opened)
        {
            Initialize();
            _device.OnReconnect(() => _ = TryChangeDeviceMode());
            return true;
        }

        if (exception is not null)
        {
            LogError(exception);
        }

        return false;
    }

    private bool TryChangeDeviceMode()
    {
        if (_isChangingDeviceMode)
        {
            LogWarning("Device mode change requested during device mode change.");
            return false;
        }

        using (_guardManager.AwaitExclusiveAccess())
        {
            _isChangingDeviceMode = true;
            LogInfo("Changing device mode to software-controlled");
            SendCommand(Commands.EnterSoftwareMode);
            _isChangingDeviceMode = false;
        }

        return true;
    }

    private void Initialize()
    {
        TryChangeDeviceMode();
        RefreshImpl(initialize: true);
    }

    public override void Disconnect()
    {
        _device.OnReconnect(null);
        _device.Close();
    }

    public override string GetFirmwareVersion()
    {
        byte[] response;

        using (_guardManager.AwaitExclusiveAccess())
        {
            response = SendCommand(Commands.ReadFirmwareVersion);
        }

        return CommanderCoreDataReader.GetFirmwareVersion(response);
    }

    public override void Refresh(CancellationToken cancellationToken = default) => RefreshImpl(initialize: false);

    private void RefreshImpl(bool initialize = false)
    {
        var connectedSpeedsResponse = ReadFromEndpoint(Endpoints.GetConnectedSpeeds, DataTypes.ConnectedSpeeds);

        if (initialize)
        {
            InitializeSpeedChannels(connectedSpeedsResponse);
        }

        WriteRequestedSpeeds();

        var speedsResponse = ReadFromEndpoint(Endpoints.GetSpeeds, DataTypes.Speeds);
        RefreshSpeeds(connectedSpeedsResponse, speedsResponse);

        var temperaturesResponse = ReadFromEndpoint(Endpoints.GetTemperatures, DataTypes.Temperatures);
        RefreshTemperatures(temperaturesResponse);

        if (CanLogDebug)
        {
            LogDebug(GetStateStringRepresentation());
        }
    }

    public override void SetChannelPower(int channel, int percent)
    {
        var clampMin = PERCENT_MIN;
        if (_firstChannelExt && channel == PUMP_CHANNEL)
        {
            clampMin = _pumpPowerMinimum;
        }

        _requestedChannelPower[channel] = (byte)Utils.Clamp(percent, clampMin, PERCENT_MAX);
    }

    public override void ResetChannel(int channel)
    {
        var value = DEFAULT_SPEED_CHANNEL_POWER;
        if (_firstChannelExt && channel == PUMP_CHANNEL)
        {
            value = DEFAULT_SPEED_CHANNEL_POWER_PUMP;
        }

        SetChannelPower(channel, value);
    }

    private void RefreshTemperatures(EndpointResponse temperaturesResponse)
    {
        var sensors = GetTemperatureSensors(temperaturesResponse);

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

    private void RefreshSpeeds(EndpointResponse connectedSpeedsResponse, EndpointResponse speedsResponse)
    {
        var sensors = GetSpeedSensors(connectedSpeedsResponse, speedsResponse);

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

    private void InitializeSpeedChannels(EndpointResponse connectedSpeedsResponse)
    {
        _speedChannelCount = CommanderCoreDataReader.GetSpeedSensorCount(connectedSpeedsResponse.Payload);
        _requestedChannelPower.Clear();

        for (int i = 0; i < _speedChannelCount; i++)
        {
            ResetChannel(i);
        }
    }

    private IReadOnlyCollection<SpeedSensor> GetSpeedSensors(EndpointResponse connectedSpeedsResponse, EndpointResponse speedsResponse)
    {
        var speedSensors = CommanderCoreDataReader.GetSpeedSensors(connectedSpeedsResponse.Payload, speedsResponse.Payload);
        var sensors = new List<SpeedSensor>(speedSensors.Count);

        foreach (var speedSensor in speedSensors)
        {
            var i = speedSensor.Channel;

            if (!_firstChannelExt)
            {
                sensors.Add(new SpeedSensor($"Fan #{i + 1}", i, speedSensor.Rpm, supportsControl: true));
            }
            else
            {
                sensors.Add(new SpeedSensor(i == PUMP_CHANNEL ? "Pump" : $"Fan #{i}", i, speedSensor.Rpm, supportsControl: true));
            }
        }

        return sensors;
    }

    private IReadOnlyCollection<TemperatureSensor> GetTemperatureSensors(EndpointResponse temperaturesResponse)
    {
        var temperatureSensors = CommanderCoreDataReader.GetTemperatureSensors(temperaturesResponse.Payload);
        var sensors = new List<TemperatureSensor>(temperatureSensors.Count);

        foreach (var temperatureSensor in temperatureSensors)
        {
            var i = temperatureSensor.Channel;

            if (!_firstChannelExt)
            {
                sensors.Add(new TemperatureSensor($"Temp #{i + 1}", i, temperatureSensor.TempCelsius));
            }
            else
            {
                sensors.Add(new TemperatureSensor(i == PUMP_CHANNEL ? "Liquid Temp" : $"Temp #{i}", i, temperatureSensor.TempCelsius));
            }
        }

        return sensors;
    }

    private void WriteRequestedSpeeds()
    {
        if (!_requestedChannelPower.ApplyChanges())
        {
            return;
        }

        var channelSpeeds = new Dictionary<int, byte>(_speedChannelCount);
        for (var channel = 0; channel < _speedChannelCount; channel++)
        {
            channelSpeeds[channel] = _requestedChannelPower[channel];
        }

        var data = CommanderCoreDataWriter.CreateSoftwareSpeedFixedPercentData(channelSpeeds);

        WriteToEndpoint(Endpoints.SoftwareSpeedFixedPercent, DataTypes.SoftwareSpeedFixedPercent, data);
    }

    private byte[] SendCommand(ReadOnlySpan<byte> command, ReadOnlySpan<byte> data = default, ReadOnlySpan<byte> waitForDataType = default)
    {
        var writeBuf = CommanderCoreDataWriter.CreateCommandPacket(_packetSize + 1, command, data);
        var readBuf = new byte[_packetSize];

        Write(writeBuf);
        Read(readBuf);

        if (waitForDataType.Length == 2)
        {
            var cts = new CancellationTokenSource(SEND_COMMAND_WAIT_FOR_DATA_TYPE_READ_TIMEOUT_MS);

            while (!cts.IsCancellationRequested && !DoesResponseDataTypeMatchExpected(readBuf, waitForDataType))
            {
                Read(readBuf);
            }

            if (cts.IsCancellationRequested)
            {
                throw CreateCommandException("Operation canceled: The expected data type was not read within the specified time.", command, data, waitForDataType);
            }
        }

        return readBuf.AsSpan(1).ToArray();
    }

    private static CorsairLinkDeviceException CreateCommandException(string message, ReadOnlySpan<byte> command, ReadOnlySpan<byte> data, ReadOnlySpan<byte> waitForDataType)
    {
        var exception = new CorsairLinkDeviceException(message);
        exception.Data[nameof(command)] = command.ToHexString();
        exception.Data[nameof(data)] = data.ToHexString();
        exception.Data[nameof(waitForDataType)] = waitForDataType.ToHexString();
        return exception;
    }

    private bool DoesResponseDataTypeMatchExpected(byte[] responseBuffer, ReadOnlySpan<byte> expectedDataType)
    {
        var resDataType = responseBuffer.AsSpan(4, 2);
        return resDataType.SequenceEqual(expectedDataType);
    }

    private EndpointResponse ReadFromEndpoint(ReadOnlySpan<byte> endpoint, ReadOnlySpan<byte> dataType)
    {
        byte[] res;

        using (_guardManager.AwaitExclusiveAccess())
        {
            SendCommand(Commands.CloseEndpoint, endpoint);
            SendCommand(Commands.OpenEndpoint, endpoint);
            res = SendCommand(Commands.Read, waitForDataType: dataType);
            SendCommand(Commands.CloseEndpoint, endpoint);
        }

        return new EndpointResponse(res, dataType);
    }

    private void WriteToEndpoint(ReadOnlySpan<byte> endpoint, ReadOnlySpan<byte> dataType, ReadOnlySpan<byte> data)
    {
        var writeBuf = CommanderCoreDataWriter.CreateWriteData(dataType, data);

        using (_guardManager.AwaitExclusiveAccess())
        {
            SendCommand(Commands.CloseEndpoint, endpoint);
            SendCommand(Commands.OpenEndpoint, endpoint);
            SendCommand(Commands.Write, writeBuf);
            SendCommand(Commands.CloseEndpoint, endpoint);
        }
    }

    private void Write(byte[] buffer)
    {
        if (CanLogDebug)
        {
            LogDebug($"WRITE: {buffer.ToHexString()}");
        }

        try
        {
            _device.Write(buffer);
        }
        catch (TimeoutException)
        {
            if (!TryChangeDeviceMode())
            {
                throw;
            }
        }
    }

    private void Read(byte[] buffer)
    {
        try
        {
            _device.Read(buffer);
        }
        catch (TimeoutException)
        {
            if (!TryChangeDeviceMode())
            {
                throw;
            }
        }

        if (CanLogDebug)
        {
            LogDebug($"READ:  {buffer.ToHexString()}");
        }
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

    private class EndpointResponse
    {
        public EndpointResponse(ReadOnlySpan<byte> payload, ReadOnlySpan<byte> dataType)
        {
            Payload = payload.ToArray();
            DataType = dataType.ToArray();
        }

        public byte[] Payload { get; }
        public byte[] DataType { get; }

        public ReadOnlySpan<byte> GetData() => Payload.AsSpan().Slice(5);
    }
}
