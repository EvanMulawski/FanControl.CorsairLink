namespace FanControl.CorsairLink;

using FanControl.Plugins;
using global::CorsairLink;
using global::CorsairLink.Synchronization;
using System.Timers;

public class CorsairLinkPlugin : IPlugin
{
    private const string LOGGER_CATEGORY_DEVICE_INIT = "Device Initialization";

    private readonly IDeviceGuardManager _deviceGuardManager;
    private readonly ILogger _logger;
    private readonly Timer _timer;
    private readonly object _timerLock = new();
    private IReadOnlyCollection<IDevice> _devices = new List<IDevice>(0);

    string IPlugin.Name => "CorsairLink";

    public CorsairLinkPlugin()
    {
        _logger = new CorsairLinkPluginLogger();
        _deviceGuardManager = new CorsairDevicesGuardManager();
        _timer = new Timer(1000)
        {
            Enabled = false,
        };
        _timer.Elapsed += new ElapsedEventHandler(OnTimerTick);
    }

    public bool IsInitialized { get; private set; }

    private void OnTimerTick(object sender, ElapsedEventArgs e)
    {
        bool lockTaken = false;

        try
        {
            Monitor.TryEnter(_timerLock, 100, ref lockTaken);
            if (lockTaken)
            {
                _logger.Flush();

                foreach (var device in _devices)
                {
                    try
                    {
                        device.Refresh();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(device.Name, $"An exception occurred refreshing device '{device.Name}' ({device.UniqueId}):");
                        _logger.Error(device.Name, ex.ToString());
                    }
                }

                _logger.Flush();
            }
        }
        finally
        {
            if (lockTaken)
            {
                Monitor.Exit(_timerLock);
            }
        }
    }

    void IPlugin.Close()
    {
        CloseImpl();
    }

    private void CloseImpl()
    {
        _logger.Flush();

        if (!IsInitialized)
        {
            return;
        }

        _timer.Enabled = false;

        foreach (var device in _devices)
        {
            device.Disconnect();
        }

        IsInitialized = false;
        _logger.Flush();
    }

    void IPlugin.Initialize()
    {
        if (IsInitialized)
        {
            CloseImpl();
        }

        var initializedDevices = new List<IDevice>();
        var devices = DeviceManager.GetSupportedDevices(_deviceGuardManager, _logger);

        foreach (var device in devices)
        {
            try
            {
                if (!device.Connect())
                {
                    _logger.Error(LOGGER_CATEGORY_DEVICE_INIT, $"Device '{device.Name}' ({device.UniqueId}) failed to connect! This device will not be available.");
                    continue;
                }

                initializedDevices.Add(device);
            }
            catch (Exception ex)
            {
                _logger.Error(LOGGER_CATEGORY_DEVICE_INIT, $"An exception occurred attempting to initialize device '{device.Name}' ({device.UniqueId}):");
                _logger.Error(LOGGER_CATEGORY_DEVICE_INIT, ex.ToString());
            }
        }

        _logger.Flush();
        _devices = initializedDevices;
        _timer.Enabled = true;
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
            _logger.Info(LOGGER_CATEGORY_DEVICE_INIT, device.UniqueId);
            _logger.Info(device.Name, $"Firmware Version: {device.GetFirmwareVersion()}");

            AddDeviceSpeedSensors(container, device);
            AddDeviceTemperatureSensors(container, device);
            AddDeviceSpeedControllers(container, device);
        }

        _logger.Flush();
    }

    private void AddDeviceSpeedSensors(IPluginSensorsContainer container, IDevice device)
    {
        foreach (var sensor in device.SpeedSensors)
        {
            var pluginSensor = new CorsairLinkSpeedSensor(device, sensor);
            container.FanSensors.Add(pluginSensor);
            _logger.Info(device.Name, $"Sensor: {pluginSensor.Id}");
        }
    }

    private void AddDeviceSpeedControllers(IPluginSensorsContainer container, IDevice device)
    {
        foreach (var sensor in device.SpeedSensors.Where(ss => ss.SupportsControl))
        {
            var pluginController = new CorsairLinkSpeedController(device, sensor);
            container.ControlSensors.Add(pluginController);
            _logger.Info(device.Name, $"Controller: {pluginController.Id}");
        }
    }

    private void AddDeviceTemperatureSensors(IPluginSensorsContainer container, IDevice device)
    {
        foreach (var sensor in device.TemperatureSensors)
        {
            var pluginSensor = new CorsairLinkTemperatureSensor(device, sensor);
            container.TempSensors.Add(pluginSensor);
            _logger.Info(device.Name, $"Sensor: {pluginSensor.Id}");
        }
    }
}
