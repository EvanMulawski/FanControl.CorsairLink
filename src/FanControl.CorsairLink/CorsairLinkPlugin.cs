namespace FanControl.CorsairLink;

using FanControl.Plugins;
using global::CorsairLink;

public class CorsairLinkPlugin : IPlugin
{
    private readonly IPluginLogger _logger;
    private IReadOnlyCollection<IDevice> _devices = new List<IDevice>(0);

    string IPlugin.Name => "CorsairLink";

    public CorsairLinkPlugin(IPluginLogger logger)
    {
        _logger = logger;
    }

    public bool IsInitialized => _devices is not null;

    private void Log(string message)
    {
        _logger.Log($"[CorsairLink] {message}");
    }

    void IPlugin.Close()
    {
        CloseImpl();
    }

    private void CloseImpl()
    {
        if (!IsInitialized)
        {
            return;
        }

        foreach (var device in _devices)
        {
            device.Disconnect();
        }
    }

    void IPlugin.Initialize()
    {
        if (IsInitialized)
        {
            CloseImpl();
        }

        var initializedDevices = new List<IDevice>();
        var devices = DeviceManager.GetSupportedDevices();

        foreach (var device in devices)
        {
            device.Connect();

            if (!device.IsConnected)
            {
                Log($"Device '{device.DevicePath}' failed to connect! This device will not be available.");
                continue;
            }

            initializedDevices.Add(device);
        }

        _devices = initializedDevices;
    }

    void IPlugin.Load(IPluginSensorsContainer container)
    {
        if (!IsInitialized)
        {
            return;
        }

        foreach (var device in _devices)
        {
            AddDeviceFanSensors(container, device);
            AddDeviceFanControllers(container, device);
        }
    }

    private static void AddDeviceFanSensors(IPluginSensorsContainer container, IDevice device)
    {
        if (device is IFanReader fanReader)
        {
            var fanConfig = fanReader.GetFanConfiguration();

            foreach (var fanChannel in fanConfig.Channels)
            {
                var fanSensor = new CorsairLinkFanSensor(device, fanChannel.ChannelId, fanReader);
                container.FanSensors.Add(fanSensor);
            }
        }
    }

    private static void AddDeviceFanControllers(IPluginSensorsContainer container, IDevice device)
    {
        if (device is IFanController fanController)
        {
            var fanConfig = fanController.GetFanConfiguration();

            foreach (var fanChannel in fanConfig.Channels)
            {
                var fanControlSensor = new CorsairLinkFanController(device, fanChannel.ChannelId, fanController);
                container.ControlSensors.Add(fanControlSensor);
            }
        }
    }
}
