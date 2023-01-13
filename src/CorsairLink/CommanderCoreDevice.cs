using HidSharp;
using System.Buffers.Binary;

namespace CorsairLink
{
    public sealed class CommanderCoreDevice : ICommanderCore
    {
        private static class Commands
        {
            public static ReadOnlySpan<byte> Wake => new byte[] { 0x01, 0x03, 0x00, 0x02 };
            public static ReadOnlySpan<byte> Sleep => new byte[] { 0x01, 0x03, 0x00, 0x01 };
            public static ReadOnlySpan<byte> ReadFirmwareVersion => new byte[] { 0x02, 0x13 };
            public static ReadOnlySpan<byte> OpenEndpoint => new byte[] { 0x0d, 0x00 };
            public static ReadOnlySpan<byte> CloseEndpoint => new byte[] { 0x05, 0x01, 0x00 };
            public static ReadOnlySpan<byte> Read => new byte[] { 0x08, 0x00 };
            public static ReadOnlySpan<byte> Write => new byte[] { 0x06, 0x00 };
        }

        private static class Endpoints
        {
            public static ReadOnlySpan<byte> GetSpeedsEndpoint => new byte[] { 0x17 };
            public static ReadOnlySpan<byte> GetTemperaturesEndpoint => new byte[] { 0x21 };
            public static ReadOnlySpan<byte> GetConnectedSpeedsEndpoint => new byte[] { 0x1a };
            public static ReadOnlySpan<byte> HardwareSpeedModeEndpoint => new byte[] { 0x60, 0x6d };
            public static ReadOnlySpan<byte> HardwareSpeedFixedPercentEndpoint => new byte[] { 0x61, 0x6d };
        }

        private static class DataTypes
        {
            public static ReadOnlySpan<byte> SpeedsDataType => new byte[] { 0x06, 0x00 };
            public static ReadOnlySpan<byte> TemperaturesDataType => new byte[] { 0x10, 0x00 };
            public static ReadOnlySpan<byte> ConnectedSpeedsDataType => new byte[] { 0x09, 0x00 };
            public static ReadOnlySpan<byte> HardwareSpeedModeDataType => new byte[] { 0x03, 0x00 };
            public static ReadOnlySpan<byte> HardwareSpeedFixedPercentDataType => new byte[] { 0x04, 0x00 };
        }

        private readonly HidDevice _device;
        private HidStream? _stream;
        private const int REQUEST_LENGTH = 97;
        private const int RESPONSE_LENGTH = 96;

        public CommanderCoreDevice(HidDevice device)
        {
            _device = device;
            Name = $"{device.GetProductName()} ({device.GetSerialNumber()})";
        }

        public string DevicePath => _device.DevicePath;

        public string Name { get; }

        public bool IsConnected => _stream is not null;

        public void Connect()
        {
            if (IsConnected)
            {
                return;
            }

            var openConfig = new OpenConfiguration();
            openConfig.SetOption(OpenOption.Exclusive, true);
            openConfig.SetOption(OpenOption.Transient, true);
            openConfig.SetOption(OpenOption.Interruptible, true);

            _device.TryOpen(openConfig, out _stream);
        }

        public void Disconnect()
        {
            if (!IsConnected)
            {
                return;
            }

            _stream?.Dispose();
            _stream = null;
        }

        private void ThrowIfNotConnected()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Not connected!");
            }
        }

        public string GetFirmwareVersion()
        {
            ThrowIfNotConnected();

            SendCommand(_stream!, Commands.Wake);
            var response = SendCommand(_stream!, Commands.ReadFirmwareVersion);
            SendCommand(_stream!, Commands.Sleep);

            var v1 = (int)response[3];
            var v2 = (int)response[4];
            var v3 = (int)response[5];

            return $"{v1}.{v2}.{v3}";
        }

        public SpeedSensorReport GetSpeeds()
        {
            throw new NotImplementedException();
        }

        public TemperatureSensorReport GetTemperatures()
        {
            ThrowIfNotConnected();

            SendCommand(_stream!, Commands.Wake);
            var response = ReadFromEndpoint(_stream!, Endpoints.GetTemperaturesEndpoint, DataTypes.TemperaturesDataType);
            SendCommand(_stream!, Commands.Sleep);
            response.ThrowIfInvalid();

            var sensorCount = response.Data![0];
            var sensorData = new List<TemperatureSensorData>(sensorCount);

            for (int i = 0, c = 1; i < sensorCount; i++, c += 3)
            {
                int? temp = default;
                var connected = response.Data[c] == 0x0;

                if (connected)
                {
                    temp = BinaryPrimitives.ReadInt16LittleEndian(response.Data.AsSpan().Slice(c + 1, 2)) / 10;
                }

                sensorData.Add(new TemperatureSensorData($"Temp #{i + 1}", i, temp));
            }

            return new TemperatureSensorReport(sensorData);
        }

        public void SetSpeed(int channel, int percent)
        {
            throw new NotImplementedException();
        }

        private static byte[] SendCommand(HidStream stream, ReadOnlySpan<byte> command, ReadOnlySpan<byte> data = default)
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

            stream.Write(writeBuf);
            var readBuf = new byte[RESPONSE_LENGTH];
            do
            {
                stream.Read(readBuf);
            }
            while (readBuf[0] != 0x0);

            return readBuf.AsSpan().Slice(1).ToArray();
        }

        private static EndpointResponse ReadFromEndpoint(HidStream stream, ReadOnlySpan<byte> endpoint, ReadOnlySpan<byte> dataType)
        {
            SendCommand(stream, Commands.OpenEndpoint, endpoint);
            var res = SendCommand(stream, Commands.Read).AsSpan();
            SendCommand(stream, Commands.CloseEndpoint);

            var resDataType = res.Slice(3, 2);
            if (!resDataType.SequenceEqual(dataType))
            {
                return new EndpointResponse(false, default);
            }

            return new EndpointResponse(true, res.Slice(5).ToArray());
        }

        private class EndpointResponse
        {
            public EndpointResponse(bool valid, byte[]? data)
            {
                Valid = valid;
                Data = data;
            }

            public bool Valid { get; }
            public byte[]? Data { get; }

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
