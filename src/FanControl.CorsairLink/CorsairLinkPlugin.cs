namespace FanControl.CorsairLink;

using FanControl.Plugins;
using global::CorsairLink;

public class CorsairLinkPlugin : IPlugin
{
    private readonly IPluginLogger _logger;
    private IReadOnlyCollection<IDevice2> _devices = new List<IDevice2>(0);

    string IPlugin.Name => "CorsairLink";

    public CorsairLinkPlugin(IPluginLogger logger)
    {
        _logger = logger;
    }

    public bool IsInitialized { get; private set; }

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

        IsInitialized = false;
    }

    void IPlugin.Initialize()
    {
        if (IsInitialized)
        {
            CloseImpl();
        }

        var initializedDevices = new List<IDevice2>();
        var devices = DeviceManager.GetSupportedDevices();

        foreach (var device in devices)
        {
            if (!device.Connect())
            {
                Log($"Device '{device.UniqueId}' failed to connect! This device will not be available.");
                continue;
            }

            initializedDevices.Add(device);
        }

        _devices = initializedDevices;
        IsInitialized = true;
    }

    void IPlugin.Load(IPluginSensorsContainer container)
    {
        if (!IsInitialized)
        {
            return;
        }

        foreach (var device in _devices)
        {
            AddDeviceSpeedSensors(container, device);
            AddDeviceSpeedControllers(container, device);
            AddDeviceTemperatureSensors(container, device);
        }
    }

    private void AddDeviceSpeedSensors(IPluginSensorsContainer container, IDevice2 device)
    {
        foreach (var sensor in device.SpeedSensors)
        {
            var pluginSensor = new CorsairLinkSpeedSensor(device, sensor);
            container.FanSensors.Add(pluginSensor);
        }
    }

    private void AddDeviceSpeedControllers(IPluginSensorsContainer container, IDevice2 device)
    {
        foreach (var sensor in device.SpeedSensors)
        {
            var pluginController = new CorsairLinkSpeedController(device, sensor);
            container.ControlSensors.Add(pluginController);
        }
    }

    private void AddDeviceTemperatureSensors(IPluginSensorsContainer container, IDevice2 device)
    {
        foreach (var sensor in device.TemperatureSensors)
        {
            var pluginSensor = new CorsairLinkTemperatureSensor(device, sensor);
            container.TempSensors.Add(pluginSensor);
        }
    }
}
