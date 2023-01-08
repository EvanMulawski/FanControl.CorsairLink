using CorsairLink;
using FanControl.Plugins;

namespace FanControl.CorsairLink
{
    public class CorsairLinkFanController : IPluginControlSensor
    {
        private readonly int _fanChannelId;
        private readonly IFanController _fanController;

        private float? _value;

        public CorsairLinkFanController(IDeviceInfo deviceInfo, int fanChannelId, IFanController fanController)
        {
            _fanChannelId = fanChannelId;
            _fanController = fanController;

            Id = $"CorsairLink/{deviceInfo.DevicePath}/FanController/{fanChannelId}";
            Name = $"{deviceInfo.Name} Fan #{fanChannelId + 1}";
        }

        public string Id { get; }

        public string Name { get; }

        public float? Value { get; private set; }

        public void Reset()
        {
            _value = null;
            _fanController.SetFanPower(_fanChannelId, 50);
        }

        public void Set(float val)
        {
            _value = val;
            _fanController.SetFanPower(_fanChannelId, (int)val);
        }

        public void Update()
        {
            Value = _value;
        }
    }
}
