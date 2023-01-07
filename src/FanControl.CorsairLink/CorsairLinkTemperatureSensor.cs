using CorsairLink;
using FanControl.Plugins;

namespace FanControl.CorsairLink
{
    public sealed class CorsairLinkTemperatureSensor : IPluginSensor
    {
        private readonly TemperatureSensorChannel _temperatureSensorChannel;
        private readonly ITemperatureSensorReader _temperatureReader;

        public CorsairLinkTemperatureSensor(IDeviceInfo deviceInfo, TemperatureSensorChannel temperatureSensorChannel, ITemperatureSensorReader temperatureReader)
        {
            _temperatureSensorChannel = temperatureSensorChannel;
            _temperatureReader = temperatureReader;

            Id = $"CorsairLink/{deviceInfo.DevicePath}/TemperatureSensor/{temperatureSensorChannel.ChannelId}";
            Name = $"{deviceInfo.Name} Temp #{temperatureSensorChannel.Name}";
        }

        public string Id { get; }

        public string Name { get; }

        public float? Value { get; private set; }

        public void Update()
        {
            Value = _temperatureSensorChannel.Status == TemperatureSensorStatus.Connected
                ? _temperatureReader.GetTemperatureSensorValue(_temperatureSensorChannel.ChannelId)
                : null;
        }
    }
}
