using CorsairLink;
using FanControl.Plugins;

namespace FanControl.CorsairLink;

public sealed class CorsairLinkSpeedSensor : IPluginSensor
{
    private readonly SpeedSensor _sensor;

    public CorsairLinkSpeedSensor(IDevice device, SpeedSensor sensor)
    {
        _sensor = sensor;

        Id = $"CorsairLink/{device.UniqueId}/SpeedSensor/{sensor.Channel}";
        Name = $"{device.Name} {sensor.Name}";
    }

    public string Id { get; }

    public string Name { get; }

    public float? Value { get; private set; }

    public void Update()
    {
        Value = _sensor.Rpm;
    }
}
