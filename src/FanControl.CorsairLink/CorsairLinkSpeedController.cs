using CorsairLink;
using FanControl.Plugins;

namespace FanControl.CorsairLink;

public sealed class CorsairLinkSpeedController : IPluginControlSensor
{
    private readonly IDevice _device;
    private readonly SpeedSensor _sensor;
    private float? _value;

    public CorsairLinkSpeedController(IDevice device, SpeedSensor sensor)
    {
        _device = device;
        _sensor = sensor;
        Id = $"CorsairLink/{device.UniqueId}/SpeedController/{sensor.Channel}";
        Name = $"{device.Name} {sensor.Name}";
        _sensor = sensor;
    }

    public string Id { get; }

    public string Name { get; }

    public float? Value { get; private set; }

    public void Reset()
    {
        _value = null;
        _device.ResetChannel(_sensor.Channel);
    }

    public void Set(float val)
    {
        _value = val;
        _device.SetChannelPower(_sensor.Channel, (int)val);
    }

    public void Update()
    {
        Value = _value;
    }
}
