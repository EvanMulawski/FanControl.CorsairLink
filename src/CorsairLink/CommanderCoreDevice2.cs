using HidSharp;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace CorsairLink;

public sealed class CommanderCoreDevice2 : IDevice2
{
    private static class Commands
    {
        public static ReadOnlySpan<byte> Prepare => new byte[] { 0x01, 0x03, 0x00, 0x02 };
        public static ReadOnlySpan<byte> Done => new byte[] { 0x01, 0x03, 0x00, 0x01 };
        public static ReadOnlySpan<byte> ReadFirmwareVersion => new byte[] { 0x02, 0x13 };
        public static ReadOnlySpan<byte> OpenEndpoint => new byte[] { 0x0d, 0x00 };
        public static ReadOnlySpan<byte> CloseEndpoint => new byte[] { 0x05, 0x01, 0x00 };
        public static ReadOnlySpan<byte> Read => new byte[] { 0x08, 0x00 };
        public static ReadOnlySpan<byte> Write => new byte[] { 0x06, 0x00 };
    }

    private static class Endpoints
    {
        public static ReadOnlySpan<byte> GetSpeeds => new byte[] { 0x17 };
        public static ReadOnlySpan<byte> GetConnectedSpeeds => new byte[] { 0x1a };
        public static ReadOnlySpan<byte> GetTemperatures => new byte[] { 0x21 };
        public static ReadOnlySpan<byte> HardwareSpeedMode => new byte[] { 0x60, 0x6d };
        public static ReadOnlySpan<byte> HardwareSpeedFixedPercent => new byte[] { 0x61, 0x6d };
    }

    private static class DataTypes
    {
        public static ReadOnlySpan<byte> Speeds => new byte[] { 0x06, 0x00 };
        public static ReadOnlySpan<byte> ConnectedSpeeds => new byte[] { 0x09, 0x00 };
        public static ReadOnlySpan<byte> Temperatures => new byte[] { 0x10, 0x00 };
        public static ReadOnlySpan<byte> HardwareSpeedMode => new byte[] { 0x03, 0x00 };
        public static ReadOnlySpan<byte> HardwareSpeedFixedPercent => new byte[] { 0x04, 0x00 };
    }

    private const int REQUEST_LENGTH = 97;
    private const int RESPONSE_LENGTH = 96;
    private const int WRITE_COMMAND_DATA_START_IDX = 4;

    private readonly HidDevice _device;
    private HidStream? _stream;
    private byte _speedChannelCount;
    private readonly bool _firstChannelExt;
    private readonly Dictionary<int, byte> _requestedChannelPower = new();
    private readonly Dictionary<int, SpeedSensor> _speedSensors = new();
    private readonly Dictionary<int, TemperatureSensor> _temperatureSensors = new();

    public CommanderCoreDevice2(HidDevice device)
    {
        _device = device;
        Name = $"{device.GetProductName()} ({device.GetSerialNumber()})";

        _firstChannelExt = device.ProductID == HardwareIds.CorsairCommanderCoreProductId
            || device.ProductID == HardwareIds.CorsairCommanderSTProductId;
    }

    public string UniqueId => _device.DevicePath;

    public string Name { get; }

    public IReadOnlyCollection<SpeedSensor> SpeedSensors => _speedSensors.Values;

    public IReadOnlyCollection<TemperatureSensor> TemperatureSensors => _temperatureSensors.Values;

    public bool Connect()
    {
        Disconnect();

        var openConfig = new OpenConfiguration();
        openConfig.SetOption(OpenOption.Exclusive, true);
        openConfig.SetOption(OpenOption.Transient, true);
        openConfig.SetOption(OpenOption.Interruptible, true);

        if (_device.TryOpen(openConfig, out _stream))
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

    public string GetFirmwareVersion()
    {
        ThrowIfNotConnected();

        var response = SendCommand(_stream!, Commands.ReadFirmwareVersion);

        var v1 = (int)response[3];
        var v2 = (int)response[4];
        var v3 = (int)response[5];

        return $"{v1}.{v2}.{v3}";
    }

    private void Initialize()
    {
        Prepare();

        var hardwareFixedSpeedPercentResponse = ReadFromEndpoint(_stream!, Endpoints.HardwareSpeedFixedPercent, DataTypes.HardwareSpeedFixedPercent);
        var connectedSpeedsResponse = ReadFromEndpoint(_stream!, Endpoints.GetConnectedSpeeds, DataTypes.ConnectedSpeeds);
        var speedsResponse = ReadFromEndpoint(_stream!, Endpoints.GetSpeeds, DataTypes.Speeds);
        var temperaturesResponse = ReadFromEndpoint(_stream!, Endpoints.GetTemperatures, DataTypes.Temperatures);

        InitializeRequestedChannelPower(hardwareFixedSpeedPercentResponse);
        RefreshSpeeds(connectedSpeedsResponse, speedsResponse);
        RefreshTemperatures(temperaturesResponse);

        Done();
    }

    public void Refresh()
    {
        Prepare();

        var connectedSpeedsResponse = ReadFromEndpoint(_stream!, Endpoints.GetConnectedSpeeds, DataTypes.ConnectedSpeeds);
        var speedsResponse = ReadFromEndpoint(_stream!, Endpoints.GetSpeeds, DataTypes.Speeds);
        var temperaturesResponse = ReadFromEndpoint(_stream!, Endpoints.GetTemperatures, DataTypes.Temperatures);

        WriteRequestedSpeeds();
        RefreshSpeeds(connectedSpeedsResponse, speedsResponse);
        RefreshTemperatures(temperaturesResponse);

        Done();
    }

    public void SetChannelPower(int channel, int percent)
    {
        _requestedChannelPower[channel] = (byte)Utils.Clamp(percent, 0, 100);
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

    private void InitializeRequestedChannelPower(EndpointResponse hardwareFixedSpeedPercentResponse)
    {
        hardwareFixedSpeedPercentResponse.ThrowIfInvalid();

        var responseData = hardwareFixedSpeedPercentResponse.GetData();
        _speedChannelCount = responseData[0];
        _requestedChannelPower.Clear();

        for (int i = 0, s = 1; i < _speedChannelCount; i++, s += 2)
        {
            _requestedChannelPower[i] = responseData[s];
        }
    }

    private void ThrowIfNotConnected()
    {
        if (_stream is null)
        {
            throw new InvalidOperationException("Not connected!");
        }
    }

    private IReadOnlyCollection<SpeedSensor> GetSpeedSensors(EndpointResponse connectedSpeedsResponse, EndpointResponse speedsResponse)
    {
        //var connectedSpeedsResponse = ReadFromEndpoint(_stream!, Endpoints.GetConnectedSpeeds, DataTypes.ConnectedSpeeds);
        //var speedsResponse = ReadFromEndpoint(_stream!, Endpoints.GetSpeeds, DataTypes.Speeds);

        connectedSpeedsResponse.ThrowIfInvalid();
        speedsResponse.ThrowIfInvalid();

        var connectedSpeedsResponseData = connectedSpeedsResponse.GetData();
        var speedsResponseData = speedsResponse.GetData().Slice(1);
        var sensorCount = connectedSpeedsResponseData[0];
        var sensors = new List<SpeedSensor>(sensorCount);

        for (int i = 0, c = 1, s = 0; i < sensorCount; i++, c++, s += 2)
        {
            int? rpm = default;
            var connected = connectedSpeedsResponseData[c] == 0x07;

            if (connected)
            {
                rpm = BinaryPrimitives.ReadInt16LittleEndian(speedsResponseData.Slice(s, 2));
            }

            if (!_firstChannelExt)
            {
                sensors.Add(new SpeedSensor($"Fan #{i + 1}", i, rpm));
            }
            else
            {
                sensors.Add(new SpeedSensor(i == 0 ? "Pump" : $"Fan #{i}", i, rpm));
            }
        }

        return sensors;
    }

    private IReadOnlyCollection<TemperatureSensor> GetTemperatureSensors(EndpointResponse temperaturesResponse)
    {
        //var temperaturesResponse = ReadFromEndpoint(_stream!, Endpoints.GetTemperatures, DataTypes.Temperatures);

        temperaturesResponse.ThrowIfInvalid();

        var responseData = temperaturesResponse.GetData();
        var sensorCount = responseData[0];
        var sensors = new List<TemperatureSensor>(sensorCount);

        for (int i = 0, c = 1; i < sensorCount; i++, c += 3)
        {
            int? temp = default;
            var connected = responseData[c] == 0x00;

            if (connected)
            {
                temp = BinaryPrimitives.ReadInt16LittleEndian(responseData.Slice(c + 1, 2)) / 10;
            }

            if (!_firstChannelExt)
            {
                sensors.Add(new TemperatureSensor($"Temp #{i + 1}", i, temp));
            }
            else
            {
                sensors.Add(new TemperatureSensor(i == 0 ? "Liquid Temp" : $"Temp #{i}", i, temp));
            }
        }

        return sensors;
    }

    private void WriteRequestedSpeeds()
    {
        var speedFixedPercentBuf = new byte[_speedChannelCount * 2 + 1];
        var channelsSpan = speedFixedPercentBuf.AsSpan(1);

        foreach (var c in _requestedChannelPower.Keys)
        {
            channelsSpan[c * 2] = _requestedChannelPower[c];
        }

        // set all channels to fixed percent (mode = 0x00)
        WriteToEndpoint(_stream!, Endpoints.HardwareSpeedMode, DataTypes.HardwareSpeedMode, default);
        WriteToEndpoint(_stream!, Endpoints.HardwareSpeedFixedPercent, DataTypes.HardwareSpeedFixedPercent, speedFixedPercentBuf);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Done()
    {
        SendCommand(_stream!, Commands.Done);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Prepare()
    {
        SendCommand(_stream!, Commands.Prepare);
    }

    private static byte[] SendCommand(Stream stream, ReadOnlySpan<byte> command, ReadOnlySpan<byte> data = default)
    {
        var writeBuf = new byte[REQUEST_LENGTH];
        writeBuf[1] = 0x08;

        var commandSpan = writeBuf.AsSpan(2, command.Length);
        command.CopyTo(commandSpan);

        if (data.Length > 0)
        {
            var dataSpan = writeBuf.AsSpan(2 + commandSpan.Length, data.Length);
            data.CopyTo(dataSpan);
        }

        stream.Write(writeBuf, 0, writeBuf.Length);
        var readBuf = new byte[RESPONSE_LENGTH];
        do
        {
            stream.Read(readBuf, 0, readBuf.Length);
        }
        while (readBuf[0] != 0x0);

        return readBuf.AsSpan(1).ToArray();
    }

    private static EndpointResponse ReadFromEndpoint(Stream stream, ReadOnlySpan<byte> endpoint, ReadOnlySpan<byte> dataType)
    {
        SendCommand(stream, Commands.OpenEndpoint, endpoint);
        var res = SendCommand(stream, Commands.Read);
        SendCommand(stream, Commands.CloseEndpoint);

        var resDataType = res.AsSpan(3, 2);
        if (!resDataType.SequenceEqual(dataType))
        {
            return new EndpointResponse(false, default);
        }

        return new EndpointResponse(true, res);
    }

    private void WriteToEndpoint(Stream stream, ReadOnlySpan<byte> endpoint, ReadOnlySpan<byte> dataType, ReadOnlySpan<byte> data)
    {
        //_ = ReadFromEndpoint(stream, endpoint, dataType);

        var len = dataType.Length + data.Length;
        if (len > REQUEST_LENGTH - endpoint.Length - WRITE_COMMAND_DATA_START_IDX)
        {
            throw new InvalidOperationException("Length of data to write exceeds maximum buffer size.");
        }

        // [0,1] = data length
        // [2,3] = 0x00 0x00
        // [4,5] = data type
        // [6,]  = data

        var writeBuf = new byte[len + WRITE_COMMAND_DATA_START_IDX];
        BinaryPrimitives.WriteInt16LittleEndian(writeBuf.AsSpan(0, 2), (short)len);

        var dataTypeSpan = writeBuf.AsSpan(WRITE_COMMAND_DATA_START_IDX, dataType.Length);
        dataType.CopyTo(dataTypeSpan);

        var dataSpan = writeBuf.AsSpan(WRITE_COMMAND_DATA_START_IDX + dataTypeSpan.Length, data.Length);
        data.CopyTo(dataSpan);

        SendCommand(stream, Commands.OpenEndpoint, endpoint);
        SendCommand(stream, Commands.Write, writeBuf);
        SendCommand(stream, Commands.CloseEndpoint, endpoint);
    }

    private class EndpointResponse
    {
        public EndpointResponse(bool valid, byte[]? payload)
        {
            Valid = valid;
            Payload = payload;
        }

        public bool Valid { get; }
        public byte[]? Payload { get; }

        public ReadOnlySpan<byte> GetData() => Payload is null ? default : Payload.AsSpan().Slice(5);

        public void ThrowIfInvalid()
        {
            if (!Valid)
            {
                throw new FormatException("The response was not valid.");
            }
        }
    }
}
