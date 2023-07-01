namespace FanControl.CorsairLink;

using FanControl.Plugins;
using global::CorsairLink;
using global::CorsairLink.Synchronization;
using System.Runtime.InteropServices;
using System.Timers;

public class CorsairLinkPlugin : IPlugin
{
    private const string LOGGER_CATEGORY_PLUGIN = "Plugin";
    private const string LOGGER_CATEGORY_DEVICE_ENUM = "Device Enumeration";
    private const string LOGGER_CATEGORY_DEVICE_INIT = "Device Initialization";
    private const string LOGGER_MESSAGE_UNSUPPORTED_RUNTIME = "The CorsairLink plugin requires the .NET Framework version of Fan Control.";
    private const string DIALOG_MESSAGE_ERRORS_DETECTED = "CorsairLink: Multiple errors detected during this session. Review the \"CorsairLink.log\" and \"log.txt\" log files located in the Fan Control directory.";
    private const byte DIALOG_MESSAGE_ERRORS_DETECTED_COUNT = 10;

    private readonly IPluginDialog _dialog;
    private readonly IDeviceGuardManager _deviceGuardManager;
    private readonly ILogger _logger;
    private readonly Timer _timer;
    private readonly object _timerLock = new();
    private IReadOnlyCollection<IDevice> _devices = new List<IDevice>(0);
    private readonly ExclusiveTaskRunner _errorDialogAwaiter = new();
    private readonly bool _errorNotificationsDisabled;
    private byte _errorLogCount = 0;

    string IPlugin.Name => "CorsairLink";

    public CorsairLinkPlugin(IPluginDialog dialog)
    {
        _errorNotificationsDisabled = Utils.GetEnvironmentFlag("FANCONTROL_CORSAIRLINK_ERROR_NOTIFICATIONS_DISABLED");
        _logger = CreateLogger();
        _deviceGuardManager = new CorsairDevicesGuardManager();
        _timer = new Timer(1000)
        {
            Enabled = false,
        };
        _timer.Elapsed += new ElapsedEventHandler(OnTimerTick);
        _dialog = dialog;

        if (!IsRuntimeSupported())
        {
            _logger.Error(LOGGER_CATEGORY_PLUGIN, LOGGER_MESSAGE_UNSUPPORTED_RUNTIME);
            _errorDialogAwaiter.TryRunSync(() => _dialog.ShowMessageDialog(LOGGER_MESSAGE_UNSUPPORTED_RUNTIME));
        }
    }

    public bool IsInitialized { get; private set; }

    private ILogger CreateLogger()
    {
        var logger = new CorsairLinkPluginLogger();
        logger.ErrorLogged += new EventHandler<EventArgs>(OnErrorLogged);
        return logger;
    }

    private void OnErrorLogged(object sender, EventArgs e)
    {
        if (_errorNotificationsDisabled || ++_errorLogCount < DIALOG_MESSAGE_ERRORS_DETECTED_COUNT)
        {
            return;
        }

        var ack = _errorDialogAwaiter.TryRunSync(() => _dialog.ShowMessageDialog(DIALOG_MESSAGE_ERRORS_DETECTED));
        if (ack)
        {
            _errorLogCount = 0;
        }
    }

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
                        _logger.Warning(device.Name, $"An error occurred refreshing device '{device.Name}' ({device.UniqueId}):");
                        _logger.Error(device.Name, ex);
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
        var devices = GetSupportedDevices();

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
                _logger.Warning(LOGGER_CATEGORY_DEVICE_INIT, $"An error occurred initializing device '{device.Name}' ({device.UniqueId}):");
                _logger.Error(LOGGER_CATEGORY_DEVICE_INIT, ex);
            }
        }

        _logger.Flush();
        _devices = initializedDevices;
        _timer.Enabled = true;
        IsInitialized = true;
    }

    private IEnumerable<IDevice> GetSupportedDevices()
    {
        var hidDevices = HidDeviceManager.GetSupportedDevices(_deviceGuardManager, _logger);
        IEnumerable<IDevice> siUsbXpressDevices = Enumerable.Empty<IDevice>();

        try
        {
            siUsbXpressDevices = SiUsbXpressDeviceManager.GetSupportedDevices(_deviceGuardManager, _logger);
        }
        catch (Exception ex)
        {
            _logger.Warning(LOGGER_CATEGORY_DEVICE_ENUM, "Failed to enumerate SiUsbXpress devices.");
            _logger.Debug(LOGGER_CATEGORY_DEVICE_ENUM, ex);
        }

        var devices = hidDevices.Concat(siUsbXpressDevices);
        return devices;
    }

    private bool IsRuntimeSupported()
    {
        var runtime = RuntimeInformation.FrameworkDescription;
        _logger.Debug(LOGGER_CATEGORY_PLUGIN, $"Runtime: {runtime}");

        var supported = runtime.IndexOf(".NET Framework") > -1;
        if (!supported)
        {
            _logger.Error(LOGGER_CATEGORY_PLUGIN, $"Unsupported Runtime: {runtime}");
        }

        return supported;
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
