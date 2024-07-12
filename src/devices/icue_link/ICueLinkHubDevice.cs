using System.Text;

namespace CorsairLink.Devices.ICueLink;

public sealed class ICueLinkHubDevice : DeviceBase
{
    private static class Commands
    {
        private const byte HANDLE_ID = 0x01;

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
        public static ReadOnlySpan<byte> GetTemperatures => new byte[] { 0x21 };
        public static ReadOnlySpan<byte> SoftwareSpeedFixedPercent => new byte[] { 0x18 };
        public static ReadOnlySpan<byte> GetSubDevices => new byte[] { 0x36 };
    }

    private static class DataTypes
    {
        public static ReadOnlySpan<byte> Speeds => new byte[] { 0x25, 0x00 };
        public static ReadOnlySpan<byte> Temperatures => new byte[] { 0x10, 0x00 };
        public static ReadOnlySpan<byte> SoftwareSpeedFixedPercent => new byte[] { 0x07, 0x00 };
        public static ReadOnlySpan<byte> SubDevices => new byte[] { 0x21, 0x00 };
    }

    private const int DEFAULT_SPEED_CHANNEL_POWER = 50;
    private const int DEFAULT_SPEED_CHANNEL_POWER_PUMP = 100;
    private const byte PERCENT_MIN = 0;
    private const byte PERCENT_MAX = 100;
    private const int SEND_COMMAND_WAIT_FOR_DATA_TYPE_READ_TIMEOUT_MS = 500;
    private const int PACKET_SIZE = 512;
    private const int PACKET_SIZE_OUT = PACKET_SIZE + 1;

    private readonly IHidDeviceProxy _device;
    private readonly IDeviceGuardManager _guardManager;
    private readonly byte _pumpPowerMinimum;
    private bool _isChangingDeviceMode;
    private readonly ChannelTrackingStore _requestedChannelPower = new();
    private readonly Dictionary<int, SpeedSensor> _speedSensors = new();
    private readonly Dictionary<int, TemperatureSensor> _temperatureSensors = new();
    private readonly Dictionary<int, (LinkHubConnectedDevice HubDevice, KnownLinkDevice KnownDevice)> _channels = new();

    public ICueLinkHubDevice(IHidDeviceProxy device, IDeviceGuardManager guardManager, ICueLinkHubDeviceOptions options, ILogger logger)
        : base(logger)
    {
        _device = device;
        _guardManager = guardManager;

        var deviceInfo = device.GetDeviceInfo();
        Name = $"{deviceInfo.ProductName} ({deviceInfo.SerialNumber})";
        UniqueId = deviceInfo.DevicePath;

        _pumpPowerMinimum = (byte)Utils.Clamp(options.MinimumPumpPower ?? ICueLinkHubDeviceOptions.MinimumPumpPowerDefault, PERCENT_MIN, PERCENT_MAX);
    }

    public override string UniqueId { get; }

    public override string Name { get; }

    public override IReadOnlyCollection<SpeedSensor> SpeedSensors => _speedSensors.Values;

    public override IReadOnlyCollection<TemperatureSensor> TemperatureSensors => _temperatureSensors.Values;

    public override bool Connect()
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
        if (CanLogDebug)
        {
            var fw = GetFirmwareVersion();
            LogDebug($"FW: {fw}");
        }

        TryChangeDeviceMode();
        RefreshImpl(initialize: true);
    }

    public override void Disconnect()
    {
        _device.Close();
    }

    public override string GetFirmwareVersion()
    {
        byte[] response;

        using (_guardManager.AwaitExclusiveAccess())
        {
            response = SendCommand(Commands.ReadFirmwareVersion);
        }

        return LinkHubDataReader.GetFirmwareVersion(response);
    }

    public override void Refresh() => RefreshImpl(initialize: false);

    private void RefreshImpl(bool initialize = false)
    {
        if (initialize)
        {
            var subDevicesResponse = ReadFromEndpoint(Endpoints.GetSubDevices, DataTypes.SubDevices);
            InitializeChannels(subDevicesResponse);
            InitializeSpeedChannels();
        }

        WriteRequestedSpeeds();

        var speedsResponse = ReadFromEndpoint(Endpoints.GetSpeeds, DataTypes.Speeds);
        RefreshSpeeds(speedsResponse);

        var temperaturesResponse = ReadFromEndpoint(Endpoints.GetTemperatures, DataTypes.Temperatures);
        RefreshTemperatures(temperaturesResponse);

        if (CanLogDebug)
        {
            LogDebug(GetStateStringRepresentation());
        }
    }

    private void InitializeChannels(EndpointResponse subDevicesResponse)
    {
        _channels.Clear();

        var subDevices = LinkHubDataReader.GetDevices(subDevicesResponse.Payload);

        foreach (var subDevice in subDevices)
        {
            var knownDevice = KnownLinkDevices.Find((LinkDeviceType)subDevice.Type, subDevice.Model);
            if (knownDevice is null)
            {
                LogWarning($"Unsupported iCUE LINK device (type={subDevice.Type}, model={subDevice.Model}, channel={subDevice.Channel}, id={subDevice.Id})");
                continue;
            }

            _channels[subDevice.Channel] = (subDevice, knownDevice);
        }
    }

    public override void SetChannelPower(int channel, int percent)
    {
        var clampMin = PERCENT_MIN;
        if (_channels[channel].KnownDevice.IsPump)
        {
            clampMin = _pumpPowerMinimum;
        }

        _requestedChannelPower[channel] = (byte)Utils.Clamp(percent, clampMin, PERCENT_MAX);
    }

    public override void ResetChannel(int channel)
    {
        var value = DEFAULT_SPEED_CHANNEL_POWER;
        if (_channels[channel].KnownDevice.IsPump)
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

    private void RefreshSpeeds(EndpointResponse speedsResponse)
    {
        var sensors = GetSpeedSensors(speedsResponse);

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

    private void InitializeSpeedChannels()
    {
        foreach (var i in _channels.Keys)
        {
            ResetChannel(i);
        }
    }

    private IReadOnlyCollection<SpeedSensor> GetSpeedSensors(EndpointResponse speedsResponse)
    {
        var sensors = new List<SpeedSensor>();
        var parsedSensors = LinkHubDataReader.GetSpeedSensors(speedsResponse.Payload);

        foreach (var sensor in parsedSensors)
        {
            if (!_channels.TryGetValue(sensor.Channel, out var channel))
            {
                continue;
            }

            if (sensor.Status == LinkHubSpeedSensorStatus.Available)
            {
                sensors.Add(new SpeedSensor($"{sensor.Channel}: {channel.KnownDevice.Name}", sensor.Channel, sensor.Rpm, supportsControl: true));
            }
        }

        return sensors;
    }

    private IReadOnlyCollection<TemperatureSensor> GetTemperatureSensors(EndpointResponse temperaturesResponse)
    {
        var sensors = new List<TemperatureSensor>();
        var parsedSensors = LinkHubDataReader.GetTemperatureSensors(temperaturesResponse.Payload);

        foreach (var sensor in parsedSensors)
        {
            if (!_channels.TryGetValue(sensor.Channel, out var channel))
            {
                continue;
            }

            if (sensor.Status == LinkHubTemperatureSensorStatus.Available)
            {
                sensors.Add(new TemperatureSensor($"{sensor.Channel}: {channel.KnownDevice.Name}", sensor.Channel, sensor.TempCelsius));
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

        var channelSpeeds = new Dictionary<int, byte>(_channels.Count);
        foreach (var channel in _channels.Keys)
        {
            channelSpeeds[channel] = _requestedChannelPower[channel];
        }

        var data = LinkHubDataWriter.CreateSoftwareSpeedFixedPercentData(channelSpeeds);

        WriteToEndpoint(Endpoints.SoftwareSpeedFixedPercent, DataTypes.SoftwareSpeedFixedPercent, data);
    }

    private byte[] SendCommand(ReadOnlySpan<byte> command, ReadOnlySpan<byte> data = default, ReadOnlySpan<byte> waitForDataType = default)
    {
        var writeBuf = LinkHubDataWriter.CreateCommandPacket(PACKET_SIZE_OUT, command, data);
        var readBuf = new byte[PACKET_SIZE];

        Write(writeBuf);
        Read(readBuf);

        if (IsResponseError(readBuf))
        {
            throw CreateCommandException("Command error: An error code was returned after sending the command.", command, data, waitForDataType);
        }

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

    private bool IsResponseError(ReadOnlySpan<byte> responseBuffer)
    {
        var errorByte = responseBuffer[4];
        return errorByte != 0x00;
    }

    private bool DoesResponseDataTypeMatchExpected(ReadOnlySpan<byte> responseBuffer, ReadOnlySpan<byte> expectedDataType)
    {
        var resDataType = responseBuffer.Slice(5, 2);
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
        var writeBuf = LinkHubDataWriter.CreateWriteData(dataType, data);

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

        public ReadOnlySpan<byte> GetData() => Payload.AsSpan().Slice(6);
    }
}
