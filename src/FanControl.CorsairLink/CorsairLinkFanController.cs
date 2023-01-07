using CorsairLink;
using FanControl.Plugins;

namespace FanControl.CorsairLink
{
    public sealed class CorsairLinkFanController : IPluginControlSensor
    {
        private readonly FanChannel _fanChannel;
        private readonly IFanController _fanController;

        private float? _value;

        public CorsairLinkFanController(IDeviceInfo deviceInfo, FanChannel fanChannel, IFanController fanController)
        {
            _fanChannel = fanChannel;
            _fanController = fanController;

            Id = $"CorsairLink/{deviceInfo.DevicePath}/FanController/{fanChannel.ChannelId}";
            Name = $"{deviceInfo.Name} Fan #{fanChannel.Name}";
        }

        public string Id { get; }

        public string Name { get; }

        public float? Value { get; private set; }

        public void Reset()
        {
            _value = null;
            _fanController.SetFanPower(_fanChannel.ChannelId, 50);
        }

        public void Set(float val)
        {
            _value = val;
            _fanController.SetFanPower(_fanChannel.ChannelId, (int)val);
        }

        public void Update()
        {
            Value = _value;
        }
    }
}
