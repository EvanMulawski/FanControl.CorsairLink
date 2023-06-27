﻿using System.Buffers.Binary;

namespace CorsairLink.Devices;

public sealed class CommanderCoreDevice : DeviceBase
{
    private static class Commands
    {
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

    private const int REQUEST_LENGTH = 65;
    private const int RESPONSE_LENGTH = 64;
    private const int DEFAULT_SPEED_CHANNEL_POWER = 50;
    private const byte PERCENT_MIN = 0x00;
    private const byte PERCENT_MAX = 0x64;
    private const int SEND_COMMAND_WAIT_FOR_DATA_TYPE_READ_TIMEOUT_MS = 500;

    private readonly IHidDeviceProxy _device;
    private readonly IDeviceGuardManager _guardManager;
    private byte _speedChannelCount;
    private readonly bool _firstChannelExt;
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
            SendCommand(Commands.EnterSoftwareMode);
            RefreshImpl(initialize: true);
            return true;
        }

        if (exception is not null)
        {
            LogError(exception.ToString());
        }

        return false;
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

        var v1 = (int)response[3];
        var v2 = (int)response[4];
        var v3 = (int)response[5];

        return $"{v1}.{v2}.{v3}";
    }

    public override void Refresh() => RefreshImpl();

    private void RefreshImpl(bool initialize = false)
    {
        EndpointResponse connectedSpeedsResponse;
        EndpointResponse speedsResponse;
        EndpointResponse temperaturesResponse;

        using (_guardManager.AwaitExclusiveAccess())
        {
            connectedSpeedsResponse = ReadFromEndpoint(Endpoints.GetConnectedSpeeds, DataTypes.ConnectedSpeeds);
            speedsResponse = ReadFromEndpoint(Endpoints.GetSpeeds, DataTypes.Speeds);
            temperaturesResponse = ReadFromEndpoint(Endpoints.GetTemperatures, DataTypes.Temperatures);

            if (initialize)
            {
                InitializeSpeedChannels(connectedSpeedsResponse);
            }

            WriteRequestedSpeeds();
        }

        RefreshSpeeds(connectedSpeedsResponse, speedsResponse);
        RefreshTemperatures(temperaturesResponse);
    }

    public override void SetChannelPower(int channel, int percent)
    {
        _requestedChannelPower[channel] = (byte)Utils.Clamp(percent, PERCENT_MIN, PERCENT_MAX);
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
        var connectedSpeedsResponseData = connectedSpeedsResponse.GetData();
        _speedChannelCount = connectedSpeedsResponseData[0];
        _requestedChannelPower.Clear();

        for (int i = 0, s = 1; i < _speedChannelCount; i++, s += 2)
        {
            _requestedChannelPower[i] = DEFAULT_SPEED_CHANNEL_POWER;
        }
    }

    private IReadOnlyCollection<SpeedSensor> GetSpeedSensors(EndpointResponse connectedSpeedsResponse, EndpointResponse speedsResponse)
    {
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
                sensors.Add(new SpeedSensor($"Fan #{i + 1}", i, rpm, supportsControl: true));
            }
            else
            {
                sensors.Add(new SpeedSensor(i == 0 ? "Pump" : $"Fan #{i}", i, rpm, supportsControl: true));
            }
        }

        return sensors;
    }

    private IReadOnlyCollection<TemperatureSensor> GetTemperatureSensors(EndpointResponse temperaturesResponse)
    {
        var responseData = temperaturesResponse.GetData();
        var sensorCount = responseData[0];
        var sensors = new List<TemperatureSensor>(sensorCount);

        for (int i = 0, c = 1; i < sensorCount; i++, c += 3)
        {
            float? temp = default;
            var connected = responseData[c] == 0x00;

            if (connected)
            {
                temp = BinaryPrimitives.ReadInt16LittleEndian(responseData.Slice(c + 1, 2)) / 10f;
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
        if (!_requestedChannelPower.ApplyChanges())
        {
            return;
        }

        // [0] number of channels
        // [1,4] channel 0 data
        // [5,8] channel 1 data
        // [9,12] channel 2 data
        // [13,16] channel 3 data
        // [17,20] channel 4 data
        // [21,24] channel 5 data
        // [25,28] channel 6 data
        // ...

        // channel data:
        // [0] = channel id
        // [1] = 0x00
        // [2] = percent
        // [3] = 0x00

        var speedFixedPercentBuf = new byte[_speedChannelCount * 4 + 1];
        speedFixedPercentBuf[0] = _speedChannelCount;

        var channelsSpan = speedFixedPercentBuf.AsSpan(1);

        foreach (var c in _requestedChannelPower.Channels)
        {
            channelsSpan[c * 4] = (byte)c;
            channelsSpan[c * 4 + 2] = _requestedChannelPower[c];
        }

        WriteToEndpoint(Endpoints.SoftwareSpeedFixedPercent, DataTypes.SoftwareSpeedFixedPercent, speedFixedPercentBuf);
    }

    private byte[] SendCommand(ReadOnlySpan<byte> command, ReadOnlySpan<byte> data = default, ReadOnlySpan<byte> waitForDataType = default)
    {
        // [0] = 0x00
        // [1] = 0x08
        // [2,a] = command
        // [a+1,] = data

        var readBuf = new byte[RESPONSE_LENGTH];
        var writeBuf = new byte[REQUEST_LENGTH];
        writeBuf[1] = 0x08;

        var commandSpan = writeBuf.AsSpan(2, command.Length);
        command.CopyTo(commandSpan);

        if (data.Length > 0)
        {
            var dataSpan = writeBuf.AsSpan(2 + commandSpan.Length, data.Length);
            data.CopyTo(dataSpan);
        }

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
                throw new CorsairLinkDeviceException("Operation canceled. The expected data type was not read within the specified time."
                    + $" (command={command.ToHexString()}, data={data.ToHexString()}, waitForDataType={waitForDataType.ToHexString()})");
            }
        }

        return readBuf.AsSpan(1).ToArray();
    }

    private bool DoesResponseDataTypeMatchExpected(byte[] responseBuffer, ReadOnlySpan<byte> expectedDataType)
    {
        var resDataType = responseBuffer.AsSpan(4, 2);
        return resDataType.SequenceEqual(expectedDataType);
    }

    private EndpointResponse ReadFromEndpoint(ReadOnlySpan<byte> endpoint, ReadOnlySpan<byte> dataType)
    {
        SendCommand(Commands.OpenEndpoint, endpoint);
        var res = SendCommand(Commands.Read, waitForDataType: dataType);
        SendCommand(Commands.CloseEndpoint);

        return new EndpointResponse(res, dataType);
    }

    private void WriteToEndpoint(ReadOnlySpan<byte> endpoint, ReadOnlySpan<byte> dataType, ReadOnlySpan<byte> data)
    {
        const int HEADER_LENGTH = 4;

        // [0,1] = payload length
        // [2,3] = 0x00 0x00
        // [4,5] = data type
        // [6,]  = data

        var writeBuf = new byte[dataType.Length + data.Length + HEADER_LENGTH];
        BinaryPrimitives.WriteInt16LittleEndian(writeBuf.AsSpan(0, 2), (short)(data.Length + 2));
        dataType.CopyTo(writeBuf.AsSpan(HEADER_LENGTH, dataType.Length));
        data.CopyTo(writeBuf.AsSpan(HEADER_LENGTH + dataType.Length, data.Length));

        SendCommand(Commands.OpenEndpoint, endpoint);
        SendCommand(Commands.Write, writeBuf);
        SendCommand(Commands.CloseEndpoint);
    }

    private void Write(byte[] buffer)
    {
        _device.Write(buffer);
    }

    private void Read(byte[] buffer)
    {
        _device.Read(buffer);
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
