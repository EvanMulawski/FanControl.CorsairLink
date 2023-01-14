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
        public static readonly byte WriteFanSpeed = 0x24;
        public static readonly byte WriteFanCurve = 0x25;
        public static readonly byte WriteFanExternalTemp = 0x26;
        public static readonly byte WriteFanForceThreePinMode = 0x27;
        public static readonly byte WriteFanDetectionType = 0x28;
        public static readonly byte ReadFanDetectionType = 0x29;
    }

    private const int REQUEST_LENGTH = 64;
    private const int RESPONSE_LENGTH = 17;

    private readonly HidDevice _device;
    private HidStream? _stream;

    public CommanderProDevice(HidDevice device)
    {
        _device = device;
        Name = $"{device.GetProductName()} ({device.GetSerialNumber()})";
    }

    public string UniqueId => _device.DevicePath;

    public string Name { get; }

    public IReadOnlyCollection<SpeedSensor> SpeedSensors => throw new NotImplementedException();

    public IReadOnlyCollection<TemperatureSensor> TemperatureSensors => throw new NotImplementedException();

    public bool Connect()
    {
        Disconnect();

        var openConfig = new OpenConfiguration();
        openConfig.SetOption(OpenOption.Exclusive, true);
        openConfig.SetOption(OpenOption.Transient, true);
        openConfig.SetOption(OpenOption.Interruptible, true);

        return _device.TryOpen(openConfig, out _stream);
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
        _stream!.Write(request);
        var response = CreateResponse(RESPONSE_LENGTH);
        _stream!.Read(response);

        var v1 = (int)response[2];
        var v2 = (int)response[3];
        var v3 = (int)response[4];

        return $"{v1}.{v2}.{v3}";
    }

    public int GetFanRpm(int channelId)
    {
        ThrowIfNotConnected();

        var request = CreateRequest(Commands.ReadFanSpeed, REQUEST_LENGTH);
        request[2] = Convert.ToByte(Utils.Clamp(channelId, 0, 5));
        _stream!.Write(request);
        var response = CreateResponse(RESPONSE_LENGTH);
        _stream!.Read(response);

        return BinaryPrimitives.ReadInt16BigEndian(response.AsSpan().Slice(2));
    }

    public void SetFanRpm(int channelId, int rpm)
    {
        ThrowIfNotConnected();

        var request = CreateRequest(Commands.WriteFanSpeed, REQUEST_LENGTH);
        request[2] = Convert.ToByte(Utils.Clamp(channelId, 0, 5));
        BinaryPrimitives.WriteInt16BigEndian(request.AsSpan().Slice(3), Convert.ToInt16(Math.Max(0, rpm)));
        _stream!.Write(request);
        var response = CreateResponse(RESPONSE_LENGTH);
        _stream!.Read(response);
    }

    public void SetFanPower(int channelId, int percent)
    {
        ThrowIfNotConnected();

        var request = CreateRequest(Commands.WriteFanPower, REQUEST_LENGTH);
        request[2] = Convert.ToByte(Utils.Clamp(channelId, 0, 5));
        request[3] = Convert.ToByte(Utils.Clamp(percent, 0, 100));
        _stream!.Write(request);
        var response = CreateResponse(RESPONSE_LENGTH);
        _stream!.Read(response);
    }

    public int GetTemperatureSensorValue(int channelId)
    {
        ThrowIfNotConnected();

        var request = CreateRequest(Commands.ReadTemperatureValue, REQUEST_LENGTH);
        request[2] = Convert.ToByte(Utils.Clamp(channelId, 0, 3));
        _stream!.Write(request);
        var response = CreateResponse(RESPONSE_LENGTH);
        _stream!.Read(response);

        return BinaryPrimitives.ReadInt16BigEndian(response.AsSpan().Slice(2)) / 100;
    }

    public FanConfiguration GetFanConfiguration()
    {
        ThrowIfNotConnected();

        var request = CreateRequest(Commands.ReadFanMask, REQUEST_LENGTH);
        _stream!.Write(request);
        var response = CreateResponse(RESPONSE_LENGTH);
        _stream!.Read(response);

        var fan1Mode = (FanMode)response[2];
        var fan2Mode = (FanMode)response[3];
        var fan3Mode = (FanMode)response[4];
        var fan4Mode = (FanMode)response[5];
        var fan5Mode = (FanMode)response[6];
        var fan6Mode = (FanMode)response[7];

        return new FanConfiguration(new[] {
                new FanChannel(0, fan1Mode),
                new FanChannel(1, fan2Mode),
                new FanChannel(2, fan3Mode),
                new FanChannel(3, fan4Mode),
                new FanChannel(4, fan5Mode),
                new FanChannel(5, fan6Mode),
            }
        );
    }

    public TemperatureSensorConfiguration GetTemperatureSensorConfiguration()
    {
        ThrowIfNotConnected();

        var request = CreateRequest(Commands.ReadTemperatureMask, REQUEST_LENGTH);
        _stream!.Write(request);
        var response = CreateResponse(RESPONSE_LENGTH);
        _stream!.Read(response);

        var sensor1Status = (TemperatureSensorStatus)response[2];
        var sensor2Status = (TemperatureSensorStatus)response[3];
        var sensor3Status = (TemperatureSensorStatus)response[4];
        var sensor4Status = (TemperatureSensorStatus)response[5];

        return new TemperatureSensorConfiguration(new[] {
                new TemperatureSensorChannel(0, sensor1Status),
                new TemperatureSensorChannel(1, sensor2Status),
                new TemperatureSensorChannel(2, sensor3Status),
                new TemperatureSensorChannel(3, sensor4Status),
            }
        );
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

    public void Refresh()
    {
        throw new NotImplementedException();
    }

    public void SetChannelPower(int channel, int percent)
    {
        throw new NotImplementedException();
    }
}
