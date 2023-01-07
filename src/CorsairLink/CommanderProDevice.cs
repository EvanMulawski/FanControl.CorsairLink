using HidSharp;
using System.Buffers.Binary;

namespace CorsairLink;

public sealed class CommanderProDevice : ICommanderPro
{
    private readonly HidDevice _device;
    private HidStream? _stream;
    private int _requestLength;
    private int _responseLength;

    public CommanderProDevice(HidDevice device)
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

        if (_device.TryOpen(out _stream))
        {
            _requestLength = _device.GetMaxOutputReportLength();
            _responseLength = _device.GetMaxInputReportLength();
        }
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

        var request = Utils.CreateRequest(HidDeviceCommands.CommanderPro.ReadFirmwareVersion, _requestLength);
        _stream!.Write(request);
        var response = Utils.CreateResponse(_responseLength);
        _stream!.Read(response);

        var v1 = (int)response[2];
        var v2 = (int)response[3];
        var v3 = (int)response[4];

        return $"{v1}.{v2}.{v3}";
    }

    public int GetFanRpm(int channelId)
    {
        ThrowIfNotConnected();

        var request = Utils.CreateRequest(HidDeviceCommands.CommanderPro.ReadFanSpeed, _requestLength);
        request[2] = Convert.ToByte(Utils.Clamp(channelId, 0, 5));
        _stream!.Write(request);
        var response = Utils.CreateResponse(_responseLength);
        _stream!.Read(response);

        return BinaryPrimitives.ReadInt16BigEndian(response.AsSpan().Slice(2));
    }

    public void SetFanRpm(int channelId, int rpm)
    {
        ThrowIfNotConnected();

        var request = Utils.CreateRequest(HidDeviceCommands.CommanderPro.WriteFanSpeed, _requestLength);
        request[2] = Convert.ToByte(Utils.Clamp(channelId, 0, 5));
        BinaryPrimitives.WriteInt16BigEndian(request.AsSpan().Slice(3), Convert.ToInt16(Math.Max(0, rpm)));
        _stream!.Write(request);
        var response = Utils.CreateResponse(_responseLength);
        _stream!.Read(response);
    }

    public void SetFanPower(int channelId, int percent)
    {
        ThrowIfNotConnected();

        var request = Utils.CreateRequest(HidDeviceCommands.CommanderPro.WriteFanPower, _requestLength);
        request[2] = Convert.ToByte(Utils.Clamp(channelId, 0, 5));
        request[3] = Convert.ToByte(Utils.Clamp(percent, 0, 100));
        _stream!.Write(request);
        var response = Utils.CreateResponse(_responseLength);
        _stream!.Read(response);
    }

    public int GetTemperatureSensorValue(int channelId)
    {
        ThrowIfNotConnected();

        var request = Utils.CreateRequest(HidDeviceCommands.CommanderPro.ReadTemperatureValue, _requestLength);
        request[2] = Convert.ToByte(Utils.Clamp(channelId, 0, 3));
        _stream!.Write(request);
        var response = Utils.CreateResponse(_responseLength);
        _stream!.Read(response);

        return BinaryPrimitives.ReadInt16BigEndian(response.AsSpan().Slice(2)) / 100;
    }

    public FanConfiguration GetFanConfiguration()
    {
        ThrowIfNotConnected();

        var request = Utils.CreateRequest(HidDeviceCommands.CommanderPro.ReadFanMask, _requestLength);
        _stream!.Write(request);
        var response = Utils.CreateResponse(_responseLength);
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

        var request = Utils.CreateRequest(HidDeviceCommands.CommanderPro.ReadTemperatureMask, _requestLength);
        _stream!.Write(request);
        var response = Utils.CreateResponse(_responseLength);
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
}
