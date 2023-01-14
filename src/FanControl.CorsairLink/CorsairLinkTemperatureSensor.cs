using CorsairLink;
using FanControl.Plugins;

namespace FanControl.CorsairLink;

public sealed class CorsairLinkTemperatureSensor : IPluginSensor
{
    private readonly TemperatureSensor _sensor;

    public CorsairLinkTemperatureSensor(IDevice device, TemperatureSensor sensor)
    {
        _sensor = sensor;

        Id = $"CorsairLink/{device.UniqueId}/TemperatureSensor/{sensor.Channel}";
        Name = $"{device.Name} {sensor.Name}";
    }

    public string Id { get; }

    public string Name { get; }

    public float? Value { get; private set; }

    public void Update()
    {
        Value = _sensor.TemperatureCelsius;
    }
}
