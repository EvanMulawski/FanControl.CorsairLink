using CorsairLink;
using FanControl.Plugins;

namespace FanControl.CorsairLink
{
    public class CorsairLinkFanSensor : IPluginSensor
    {
        private readonly int _fanChannelId;
        private readonly IFanReader _fanReader;

        public CorsairLinkFanSensor(IDeviceInfo deviceInfo, int fanChannelId, IFanReader fanReader)
        {
            _fanChannelId = fanChannelId;
            _fanReader = fanReader;

            Id = $"CorsairLink/{deviceInfo.DevicePath}/FanSensor/{fanChannelId}";
            Name = $"{deviceInfo.Name} Fan #{fanChannelId + 1}";
        }

        public string Id { get; }

        public string Name { get; }

        public float? Value { get; private set; }

        public void Update()
        {
            Value = _fanReader.GetFanRpm(_fanChannelId);
        }
    }
}
