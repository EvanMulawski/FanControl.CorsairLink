namespace FanControl.CorsairLink;

using FanControl.Plugins;
using global::CorsairLink;
using global::CorsairLink.Synchronization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Timers;

public sealed class CorsairLinkPlugin : IPlugin
{
    private const string LOGGER_CATEGORY_PLUGIN = "Plugin";
    private const string LOGGER_CATEGORY_DEVICE_ENUM = "Device Enumeration";
    private const string LOGGER_CATEGORY_DEVICE_INIT = "Device Initialization";
    private const string LOGGER_MESSAGE_UNSUPPORTED_RUNTIME = "The CorsairLink plugin requires the .NET Framework version of Fan Control.";
    private const string DIALOG_MESSAGE_SUFFIX = "\n\nReview the \"CorsairLink.log\" and \"log.txt\" log files located in the Fan Control directory.\n\nTo disable this message, set the FANCONTROL_CORSAIRLINK_ERROR_NOTIFICATIONS_DISABLED environment variable to 1 and restart Fan Control.";
    private const string DIALOG_MESSAGE_ERRORS_DETECTED = "Multiple errors detected." + DIALOG_MESSAGE_SUFFIX;
    private const string DIALOG_MESSAGE_REFRESH_SKIPS_DETECTED = "Consecutive attempts to refresh devices have failed. A device may be unresponsive." + DIALOG_MESSAGE_SUFFIX;
    private const int DIALOG_MESSAGE_ERRORS_DETECTED_COUNT = 10;
    private const int DIALOG_MESSAGE_REFRESH_SKIPS_DETECTED_COUNT = 10;
    private const int ERROR_CHECK_TICK_COUNT = 30;

    private readonly IDeviceGuardManager _deviceGuardManager;
    private readonly ILogger _logger;
    private readonly Timer _timer;
    private readonly SemaphoreSlim _refreshSemaphore = new(1, 1);
    private readonly ExclusiveMonitor _errorDialogDispatcher = new();
    private readonly bool _errorNotificationsDisabled;
    private readonly string _pluginVersion;

    private IReadOnlyCollection<IDevice> _devices = new List<IDevice>(0);
    private CancellationTokenSource _refreshCts = new();
    private int _errorLogCount = 0;
    private int _errorLogCountFlag = 1;
    private int _refreshSkipCount = 0;
    private int _refreshSkipCountFlag = 1;
    private int _tickCount = 0;

    string IPlugin.Name => "CorsairLink";

    public CorsairLinkPlugin()
    {
        _pluginVersion = GetVersion();
        _errorNotificationsDisabled = Utils.GetEnvironmentFlag("FANCONTROL_CORSAIRLINK_ERROR_NOTIFICATIONS_DISABLED");
        _logger = CreateLogger();
        _errorDialogDispatcher.TaskCompleted += OnErrorDialogAcknowledged;
        _deviceGuardManager = new CorsairDevicesGuardManager();
        _timer = new Timer(1000)
        {
            Enabled = false,
        };
        _timer.Elapsed += new ElapsedEventHandler(OnTimerTick);
        if (!IsRuntimeSupported())
        {
            _logger.Error(LOGGER_CATEGORY_PLUGIN, LOGGER_MESSAGE_UNSUPPORTED_RUNTIME);
            ShowErrorDialog(LOGGER_MESSAGE_UNSUPPORTED_RUNTIME);
        }
    }

    public bool IsInitialized { get; private set; }

    private static string GetVersion() =>
        typeof(CorsairLinkPlugin).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "UNKNOWN";

    private ILogger CreateLogger()
    {
        var logger = new CorsairLinkPluginLogger();
        logger.ErrorLogged += new EventHandler<EventArgs>(OnErrorLogged);
        return logger;
    }

    private void OnErrorDialogAcknowledged(object sender, EventArgs e)
    {
        ResetErrorLogCounterState();
        ResetRefreshSkipCounterState();
    }

    private void ResetErrorLogCounterState()
    {
        Interlocked.Exchange(ref _errorLogCount, 0);
        Interlocked.Exchange(ref _errorLogCountFlag, 1);
    }

    private void ResetRefreshSkipCounterState()
    {
        Interlocked.Exchange(ref _refreshSkipCount, 0);
        Interlocked.Exchange(ref _refreshSkipCountFlag, 1);
    }

    private void OnErrorLogged(object sender, EventArgs e)
    {
        TryShowMultipleErrorsDetectedErrorDialog();
    }

    private bool TryShowMultipleErrorsDetectedErrorDialog()
    {
        if (_errorLogCountFlag == 0)
        {
            return false;
        }

        if (_errorNotificationsDisabled || Interlocked.Increment(ref _errorLogCount) < DIALOG_MESSAGE_ERRORS_DETECTED_COUNT)
        {
            return false;
        }

        Interlocked.Exchange(ref _errorLogCountFlag, 0);
        ShowErrorDialog(DIALOG_MESSAGE_ERRORS_DETECTED);
        return true;
    }

    private bool TryShowMultipleRefreshSkipsDetectedErrorDialog()
    {
        if (_refreshSkipCountFlag == 0)
        {
            return false;
        }

        if (_errorNotificationsDisabled || Interlocked.Increment(ref _refreshSkipCount) < DIALOG_MESSAGE_REFRESH_SKIPS_DETECTED_COUNT)
        {
            return false;
        }

        Interlocked.Exchange(ref _refreshSkipCountFlag, 0);
        ShowErrorDialog(DIALOG_MESSAGE_REFRESH_SKIPS_DETECTED);
        return true;
    }

    private void ShowErrorDialog(string message)
    {
        _errorDialogDispatcher.WaitNonBlocking(
            () => NativeMethods.MessageBox(
                IntPtr.Zero,
                message,
                "Fan Control - CorsairLink Error",
                MessageBoxFlags.MB_OK | MessageBoxFlags.MB_ICONERROR | MessageBoxFlags.MB_APPLMODAL | MessageBoxFlags.MB_SETFOREGROUND));
    }

    private async void OnTimerTick(object sender, ElapsedEventArgs e)
    {
        if (Interlocked.Increment(ref _tickCount) > ERROR_CHECK_TICK_COUNT && _errorLogCount < DIALOG_MESSAGE_ERRORS_DETECTED_COUNT)
        {
            ResetErrorLogCounterState();
            Interlocked.Exchange(ref _tickCount, 0);
        }

        try
        {
            await RefreshAsync(_refreshCts.Token);
        }
        catch (ObjectDisposedException)
        {
            // safe to ignore
        }
        catch (Exception ex)
        {
            _logger.Warning(LOGGER_CATEGORY_PLUGIN, "Refresh error - report to developer.", ex);
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        bool canRefresh;

        try
        {
            canRefresh = await _refreshSemaphore.WaitAsync(0, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (canRefresh)
        {
            try
            {
                _logger.Flush();

                var tasks = new List<Task>(_devices.Count);

                foreach (var device in _devices)
                {
                    var task = Task.Run(() =>
                    {
                        try
                        {
                            device.Refresh();
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(device.Name, $"An error occurred refreshing device '{device.Name}' ({device.UniqueId})", ex);
                        }
                    });

                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);

                ResetRefreshSkipCounterState();
                _logger.Flush();
            }
            finally
            {
                _refreshSemaphore.Release();
            }
        }
        else
        {
            _logger.Warning(LOGGER_CATEGORY_PLUGIN, "Refresh skipped - refresh already in progress.");
            TryShowMultipleRefreshSkipsDetectedErrorDialog();
            _logger.Flush();
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

        _refreshCts.Cancel();
        _refreshCts.Dispose();
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

        _refreshCts = new CancellationTokenSource();
        _logger.Info(LOGGER_CATEGORY_PLUGIN, $"Version: {_pluginVersion}");

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
            _logger.Warning(LOGGER_CATEGORY_DEVICE_ENUM, "Failed to enumerate SiUsbXpress devices. This can be ignored if no devices require this driver.");
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
