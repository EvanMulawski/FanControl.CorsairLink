namespace FanControl.CorsairLink;

using FanControl.Plugins;
using global::CorsairLink;
using global::CorsairLink.Synchronization;
using Microsoft.Win32;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Timers;

public sealed class CorsairLinkPlugin : IPlugin
{
#if NETFRAMEWORK
    private const string PLUGIN_NET_TARGET_FRAMEWORK = "NETFX";
#elif NET8_0_OR_GREATER
    private const string PLUGIN_NET_TARGET_FRAMEWORK = "NET";
#else
    private const string PLUGIN_NET_TARGET_FRAMEWORK = "UNKNOWN";
#endif

    private static readonly string RuntimeFramework = RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework") ? "NETFX" : "NET";

    private const string LOGGER_CATEGORY_PLUGIN = "Plugin";
    private const string LOGGER_CATEGORY_DEVICE_ENUM = "Device Enumeration";
    private const string LOGGER_CATEGORY_DEVICE_INIT = "Device Initialization";
    private const string DIALOG_MESSAGE_SUFFIX = "\n\nReview the \"CorsairLink.log\" and \"log.txt\" log files located in the Fan Control directory.\n\nTo disable this message, set the FANCONTROL_CORSAIRLINK_ERROR_NOTIFICATIONS_DISABLED environment variable to 1 and restart Fan Control.";
    private const string DIALOG_MESSAGE_ERRORS_DETECTED = "Multiple errors detected." + DIALOG_MESSAGE_SUFFIX;
    private const string DIALOG_MESSAGE_REFRESH_SKIPS_DETECTED = "Consecutive attempts to refresh devices have failed. A device may be unresponsive." + DIALOG_MESSAGE_SUFFIX;
    private const string DIALOG_MESSAGE_FRAMEWORK_MISMATCH = "The CorsairLink plugin build does not match the Fan Control build. Refer to the installation instructions for this plugin.\n\nThe plugin will not be initialized.";
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
    private bool _isSystemSuspending;

    public string Name => "CorsairLink";

    public CorsairLinkPlugin()
        : this(new CorsairLinkPluginLogger())
    {
        if (_logger is CorsairLinkPluginLogger corsairLinkPluginLogger)
        {
            corsairLinkPluginLogger.ErrorLogged += new EventHandler<EventArgs>(OnErrorLogged);
        }
    }

    public CorsairLinkPlugin(ILogger logger)
    {
        _logger = logger;
        _pluginVersion = GetVersion();
        _errorNotificationsDisabled = Utils.GetEnvironmentFlag("FANCONTROL_CORSAIRLINK_ERROR_NOTIFICATIONS_DISABLED");
        _errorDialogDispatcher.TaskCompleted += OnErrorDialogAcknowledged;
        _deviceGuardManager = new CorsairDevicesGuardManager();
        _timer = new Timer(1000)
        {
            Enabled = false,
        };
        _timer.Elapsed += new ElapsedEventHandler(OnTimerTick);
    }

    public bool IsInitialized { get; private set; }

    private static bool DoesRuntimeFrameworkMatchPluginTargetFramework()
        => RuntimeFramework == PLUGIN_NET_TARGET_FRAMEWORK;

    private static string GetVersion() =>
        typeof(CorsairLinkPlugin).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "UNKNOWN";

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
            () => _ = NativeMethods.MessageBox(
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
        if (_isSystemSuspending)
        {
            _logger.Warning(LOGGER_CATEGORY_PLUGIN, "Refresh skipped - system about to be suspended.");
            _logger.Flush();
            return;
        }

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
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    var task = Task.Run(() =>
                    {
                        try
                        {
                            device.Refresh(cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                return;
                            }

                            _logger.Error(device.Name, $"An error occurred refreshing device '{device.Name}' ({device.UniqueId})", ex);
                        }
                    }, cancellationToken);

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

    public void Close()
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

        SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;

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

    public void Initialize()
    {
        if (IsInitialized)
        {
            CloseImpl();
        }

        _logger.Info(LOGGER_CATEGORY_PLUGIN, $"Version: {_pluginVersion} ({PLUGIN_NET_TARGET_FRAMEWORK})");

        if (!DoesRuntimeFrameworkMatchPluginTargetFramework())
        {
            _logger.Error(LOGGER_CATEGORY_PLUGIN, $"Framework mismatch - plugin will not be initialized! (plugin={PLUGIN_NET_TARGET_FRAMEWORK},runtime={RuntimeFramework})");
            _logger.Flush();
            ShowErrorDialog(DIALOG_MESSAGE_FRAMEWORK_MISMATCH);
            return;
        }

        _refreshCts = new CancellationTokenSource();
        SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;

        var initializedDevices = new List<IDevice>();
        var devices = GetSupportedDevices();

        foreach (var device in devices)
        {
            try
            {
                if (!device.Connect(_refreshCts.Token))
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

    private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        HandlePowerModeChange(e.Mode);
    }

    private void HandlePowerModeChange(PowerModes powerMode)
    {
        if (powerMode == PowerModes.Suspend)
        {
            _isSystemSuspending = true;
            _refreshCts.Cancel();
        }
        else if (powerMode == PowerModes.Resume)
        {
            _isSystemSuspending = false;
        }
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

    public void Load(IPluginSensorsContainer container)
    {
        if (!IsInitialized)
        {
            return;
        }

        foreach (var device in _devices)
        {
            _logger.Info(LOGGER_CATEGORY_DEVICE_INIT, device.UniqueId);
            _logger.Info(device.Name, $"Firmware Version: {device.GetFirmwareVersion()}");

            AddDeviceFans(container, device);
            AddDeviceTemperatureSensors(container, device);
        }

        _logger.Flush();
    }

    private void AddDeviceFans(IPluginSensorsContainer container, IDevice device)
    {
        foreach (var sensor in device.SpeedSensors)
        {
            var pluginSensor = new CorsairLinkSpeedSensor(device, sensor);
            container.FanSensors.Add(pluginSensor);
            _logger.Info(device.Name, $"Speed Sensor: {pluginSensor.Id}");

            if (sensor.SupportsControl)
            {
                var pluginController = new CorsairLinkSpeedController(device, sensor, pluginSensor.Id);
                container.ControlSensors.Add(pluginController);
                _logger.Info(device.Name, $"Speed Controller: {pluginController.Id} (Paired: {pluginSensor.Id})");
            }
        }
    }

    private void AddDeviceTemperatureSensors(IPluginSensorsContainer container, IDevice device)
    {
        foreach (var sensor in device.TemperatureSensors)
        {
            var pluginSensor = new CorsairLinkTemperatureSensor(device, sensor);
            container.TempSensors.Add(pluginSensor);
            _logger.Info(device.Name, $"Temperature Sensor: {pluginSensor.Id}");
        }
    }
}
