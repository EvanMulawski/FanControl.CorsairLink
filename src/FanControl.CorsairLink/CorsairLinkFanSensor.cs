using CorsairLink;
using FanControl.Plugins;

namespace FanControl.CorsairLink
{
    public sealed class CorsairLinkFanSensor : IPluginSensor
    {
        private readonly int _fanChannelId;
        private readonly IFanReader _fanReader;

        public CorsairLinkFanSensor(IDeviceInfo deviceInfo, FanChannel fanChannel, IFanReader fanReader)
        {
            _fanChannelId = fanChannel.ChannelId;
            _fanReader = fanReader;

            Id = $"CorsairLink/{deviceInfo.DevicePath}/FanSensor/{_fanChannelId}";
            Name = $"{deviceInfo.Name} Fan #{fanChannel.Name}";
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
