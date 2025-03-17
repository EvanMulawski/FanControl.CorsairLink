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
        public static ReadOnlySpan<byte> Continuation => new byte[] { };
    }

    private static class ResponseStatuses
    {
        public const byte Ok = 0x00;
        public const byte IncorrectModeError = 0x03;
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
    private bool _supportsAdditionalSubDevices;
    private bool _needsDeviceModeChange;

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
            _ = SendCommand(Commands.EnterSoftwareMode);
            _isChangingDeviceMode = false;
            _needsDeviceModeChange = false;
        }

        return true;
    }

    private void Initialize()
    {
        var fw = GetFirmwareVersion();

        if (CanLogDebug)
        {
            LogDebug($"FW: {fw}");
        }

        // firmware v2.5 or above should have proper support for 24 devices using two endpoint reads instead of one
        var fwParts = fw.Split('.').Select(x => Convert.ToInt32(x)).ToArray();
        _supportsAdditionalSubDevices = fwParts[0] >= 2 && fwParts[1] >= 5;

        if (CanLogDebug)
        {
            LogDebug($"FW supports additional subdevices: {_supportsAdditionalSubDevices}");
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

    public override void Refresh(CancellationToken cancellationToken = default) => RefreshImpl(initialize: false);

    private void RefreshImpl(bool initialize = false)
    {
        if (initialize)
        {
            var subDevicesResponses = GetSubDevicesResponses();
            InitializeChannels(subDevicesResponses[0]!, subDevicesResponses[1]);
            InitializeSpeedChannels();
        }

        if (_needsDeviceModeChange)
        {
            TryChangeDeviceMode();
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

    private EndpointResponse?[] GetSubDevicesResponses()
    {
        if (!_supportsAdditionalSubDevices)
        {
            return [
                ReadFromEndpoint(Endpoints.GetSubDevices, DataTypes.SubDevices),
                null,
            ];
        }

        byte[] res1, res2;

        using (_guardManager.AwaitExclusiveAccess())
        {
            SendCommand(Commands.CloseEndpoint, Endpoints.GetSubDevices);
            SendCommand(Commands.OpenEndpoint, Endpoints.GetSubDevices);
            res1 = SendCommand(Commands.Read, waitForDataType: DataTypes.SubDevices);
            res2 = SendCommand(Commands.Read);
            SendCommand(Commands.CloseEndpoint, Endpoints.GetSubDevices);
        }

        return [
            new EndpointResponse(res1, DataTypes.SubDevices),
            new EndpointResponse(res2, DataTypes.Continuation),
        ];
    }

    private void InitializeChannels(EndpointResponse subDevicesResponse, EndpointResponse? subDevicesContinuationResponse)
    {
        _channels.Clear();

        var subDevices = LinkHubDataReader.GetDevices(subDevicesResponse.Payload, subDevicesContinuationResponse?.Payload ?? []);

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

        var (isError, errorCode) = GetResponseError(readBuf);

        if (isError)
        {
            if (errorCode == ResponseStatuses.IncorrectModeError)
            {
                _needsDeviceModeChange = true;
            }

            throw CreateCommandException("Command error: An error code was returned after sending the command.", command, data, waitForDataType, errorCode, writeBuf, readBuf);
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

    private static CorsairLinkDeviceException CreateCommandException(
        string message,
        ReadOnlySpan<byte> command,
        ReadOnlySpan<byte> data,
        ReadOnlySpan<byte> waitForDataType = default,
        byte errorCode = default,
        ReadOnlySpan<byte> writeBuffer = default,
        ReadOnlySpan<byte> readBuffer = default)
    {
        var exception = new CorsairLinkDeviceException(message);
        exception.Data[nameof(command)] = command.ToHexString();
        exception.Data[nameof(data)] = data.ToHexString();
        if (!waitForDataType.IsEmpty)
        {
            exception.Data[nameof(waitForDataType)] = waitForDataType.ToHexString();
        }
        if (errorCode > 0)
        {
            exception.Data[nameof(errorCode)] = errorCode.ToHexString();
        }
        if (!writeBuffer.IsEmpty)
        {
            exception.Data[nameof(writeBuffer)] = writeBuffer.ToHexString();
        }
        if (!readBuffer.IsEmpty)
        {
            exception.Data[nameof(readBuffer)] = readBuffer.ToHexString();
        }
        return exception;
    }

    private (bool IsError, byte ErrorCode) GetResponseError(ReadOnlySpan<byte> responseBuffer)
    {
        var errorByte = responseBuffer[4];
        return (errorByte != ResponseStatuses.Ok, errorByte);
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

        public ReadOnlySpan<byte> GetData() => Payload.AsSpan().Slice(4 + DataType.Length);
    }
}
