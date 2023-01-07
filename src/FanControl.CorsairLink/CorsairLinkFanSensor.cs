using CorsairLink;
using FanControl.Plugins;

namespace FanControl.CorsairLink
{
    public sealed class CorsairLinkFanSensor : IPluginSensor
    {
        private readonly FanChannel _fanChannel;
        private readonly IFanReader _fanReader;

        public CorsairLinkFanSensor(IDeviceInfo deviceInfo, FanChannel fanChannel, IFanReader fanReader)
        {
            _fanChannel = fanChannel;
            _fanReader = fanReader;

            Id = $"CorsairLink/{deviceInfo.DevicePath}/FanSensor/{fanChannel.ChannelId}";
            Name = $"{deviceInfo.Name} Fan #{fanChannel.Name}";
        }

        public string Id { get; }

        public string Name { get; }

        public float? Value { get; private set; }

        public void Update()
        {
            Value = _fanChannel.Mode != FanMode.Unknown
                ? _fanReader.GetFanRpm(_fanChannel.ChannelId)
                : null;
        }
    }
}
