using HidSharp;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Timers;

namespace CorsairLink
{
    public sealed class CommanderCoreDevice : ICommanderCore
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

        private readonly HidDevice _device;
        private HidStream? _stream;
        private byte _speedChannelCount;
        private readonly bool _firstChannelExt;
        private readonly Dictionary<int, (byte, byte)> _channelFixedPercentSpeeds = new();
        private readonly System.Timers.Timer _timer;
        private readonly object _lock = new object();

        private const int REQUEST_LENGTH = 97;
        private const int RESPONSE_LENGTH = 96;
        private const int WRITE_COMMAND_DATA_START_IDX = 4;

        public CommanderCoreDevice(HidDevice device)
        {
            _device = device;
            Name = $"{device.GetProductName()} ({device.GetSerialNumber()})";

            _firstChannelExt = device.ProductID == HardwareIds.CorsairCommanderCoreProductId
                || device.ProductID == HardwareIds.CorsairCommanderSTProductId;

            _timer = new System.Timers.Timer(1000)
            {
                Enabled = false,
            };
            _timer.Elapsed += new ElapsedEventHandler(OnTimerTick);
        }

        private void OnTimerTick(object sender, ElapsedEventArgs e)
        {
            bool lockTaken = false;

            try
            {
                Monitor.TryEnter(_lock, ref lockTaken);
                if (lockTaken)
                {
                    WriteAllSpeeds();
                }
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(_lock);
                }
            }
        }

        public string DevicePath => _device.DevicePath;

        public string Name { get; }

        public bool Connect()
        {
            Disconnect();

            var openConfig = new OpenConfiguration();
            openConfig.SetOption(OpenOption.Exclusive, true);
            openConfig.SetOption(OpenOption.Transient, true);
            openConfig.SetOption(OpenOption.Interruptible, true);

            if (_device.TryOpen(openConfig, out _stream))
            {
                SetUpSpeedChannels();
                _timer.Enabled = true;
                return true;
            }

            return false;
        }

        public void Disconnect()
        {
            _timer.Enabled = false;
            _timer.Dispose();
            _stream?.Dispose();
            _stream = null;
        }

        private void SetUpSpeedChannels()
        {
            Prepare();
            var response = ReadFromEndpoint(_stream!, Endpoints.GetTemperatures, DataTypes.Temperatures);
            Done();
            response.ThrowIfInvalid();

            var responseData = response.GetData();
            _speedChannelCount = responseData[0];
            _channelFixedPercentSpeeds.Clear();

            for (int i = 0, s = 1; i < _speedChannelCount; i++, s += 2)
            {
                _channelFixedPercentSpeeds[i] = (responseData[s], responseData[s + 1]);
            }
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

            var response = SendCommand(_stream!, Commands.ReadFirmwareVersion);

            var v1 = (int)response[3];
            var v2 = (int)response[4];
            var v3 = (int)response[5];

            return $"{v1}.{v2}.{v3}";
        }

        public SpeedSensorReport GetSpeeds()
        {
            ThrowIfNotConnected();

            Prepare();
            var connectedSpeedsResponse = ReadFromEndpoint(_stream!, Endpoints.GetConnectedSpeeds, DataTypes.ConnectedSpeeds);
            var speedsResponse = ReadFromEndpoint(_stream!, Endpoints.GetSpeeds, DataTypes.Speeds);
            Done();
            connectedSpeedsResponse.ThrowIfInvalid();
            speedsResponse.ThrowIfInvalid();

            var connectedSpeedsResponseData = connectedSpeedsResponse.GetData();
            var speedsResponseData = speedsResponse.GetData().Slice(1);
            var sensorCount = connectedSpeedsResponseData[0];
            var sensorData = new List<SpeedSensorData>(sensorCount);

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
                    sensorData.Add(new SpeedSensorData($"Fan #{i + 1}", i, rpm)); 
                }
                else
                {
                    sensorData.Add(new SpeedSensorData(i == 0 ? "Pump" : $"Fan #{i}", i, rpm));
                }
            }

            return new SpeedSensorReport(sensorData);
        }

        public TemperatureSensorReport GetTemperatures()
        {
            ThrowIfNotConnected();

            Prepare();
            var response = ReadFromEndpoint(_stream!, Endpoints.GetTemperatures, DataTypes.Temperatures);
            Done();
            response.ThrowIfInvalid();

            var responseData = response.GetData();
            var sensorCount = responseData[0];
            var sensorData = new List<TemperatureSensorData>(sensorCount);

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
                    sensorData.Add(new TemperatureSensorData($"Temp #{i + 1}", i, temp));
                }
                else
                {
                    sensorData.Add(new TemperatureSensorData(i == 0 ? "Liquid Temp" : $"Temp #{i}", i, temp));
                }
            }

            return new TemperatureSensorReport(sensorData);
        }

        public void SetSpeed(int channel, int percent)
        {
            var bytes = new byte[2];
            BinaryPrimitives.WriteInt16LittleEndian(bytes, (short)Utils.Clamp(percent, 0, 100));
            _channelFixedPercentSpeeds[channel] = (bytes[0], bytes[1]);
        }

        private void WriteAllSpeeds()
        {
            var speedFixedPercentBuf = new byte[_speedChannelCount * 2 + 1];
            var channelsSpan = speedFixedPercentBuf.AsSpan(1);

            foreach (var c in _channelFixedPercentSpeeds.Keys)
            {
                var percentBytes = _channelFixedPercentSpeeds[c];
                channelsSpan[c] = percentBytes.Item1;
                channelsSpan[c + 1] = percentBytes.Item2;
            }

            Prepare();
            // set all channels to fixed percent (mode = 0x00)
            WriteToEndpoint(_stream!, Endpoints.HardwareSpeedMode, DataTypes.HardwareSpeedMode, default);
            WriteToEndpoint(_stream!, Endpoints.HardwareSpeedFixedPercent, DataTypes.HardwareSpeedFixedPercent, speedFixedPercentBuf);
            Done();
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
}
