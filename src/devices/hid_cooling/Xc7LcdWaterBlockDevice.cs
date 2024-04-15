namespace CorsairLink.Devices.HidCooling;

public sealed class Xc7LcdWaterBlockDevice : DeviceBase
{
    private const int LIQUID_TEMP_CHANNEL = 0;
    private const int BUFFER_SIZE = 33;

    private readonly IHidDeviceProxy _device;
    private readonly IDeviceGuardManager _guardManager;
    private readonly Dictionary<int, SpeedSensor> _speedSensors = new(0);
    private readonly Dictionary<int, TemperatureSensor> _temperatureSensors = new(1);

    public Xc7LcdWaterBlockDevice(IHidDeviceProxy device, IDeviceGuardManager guardManager, ILogger logger)
        : base(logger)
    {
        _device = device;
        _guardManager = guardManager;
        _temperatureSensors.Add(LIQUID_TEMP_CHANNEL, new TemperatureSensor("Liquid Temp", LIQUID_TEMP_CHANNEL, default));

        var deviceInfo = device.GetDeviceInfo();
        Name = $"{deviceInfo.ProductName} ({deviceInfo.SerialNumber})";
        UniqueId = deviceInfo.DevicePath;
    }

    public override string UniqueId { get; }

    public override string Name { get; }

    public override IReadOnlyCollection<SpeedSensor> SpeedSensors => _speedSensors.Values;

    public override IReadOnlyCollection<TemperatureSensor> TemperatureSensors => _temperatureSensors.Values;

    private TemperatureSensor LiquidTemperatureSensor => _temperatureSensors[LIQUID_TEMP_CHANNEL];

    public static byte[] CreateFeatureBuffer(byte reportId)
    {
        var buffer = new byte[BUFFER_SIZE];
        buffer[0] = reportId;
        return buffer;
    }

    public override bool Connect()
    {
        LogDebug("Connect");

        Disconnect();

        var (opened, exception) = _device.Open();
        if (opened)
        {
            Initialize();
            return true;
        }

        if (exception is not null)
        {
            LogError(exception);
        }

        return false;
    }

    public override void Disconnect()
    {
        LogDebug("Disconnect");

        _device.Close();
    }

    public override string GetFirmwareVersion()
    {
        LogDebug("GetFirmwareVersion");

        var buffer = CreateFeatureBuffer(0x05);

        using (_guardManager.AwaitExclusiveAccess())
        {
            InvokeDevice(buffer);
        }

        return Xc7LcdWaterBlockDataReader.GetFirmwareVersion(buffer);
    }

    public override void Refresh()
    {
        LogDebug("Refresh");

        var buffer = CreateFeatureBuffer(0x18);

        using (_guardManager.AwaitExclusiveAccess())
        {
            InvokeDevice(buffer);
        }

        LiquidTemperatureSensor.TemperatureCelsius = Xc7LcdWaterBlockDataReader.GetLiquidTemperature(buffer);
    }

    private void Initialize()
    {
        Refresh();
    }

    private void InvokeDevice(byte[] buffer)
    {
        if (CanLogDebug)
        {
            LogDebug($"WRITE_FEATURE: {buffer.ToHexString()}");
        }

        _device.GetFeature(buffer);

        if (CanLogDebug)
        {
            LogDebug($"READ_FEATURE:  {buffer.ToHexString()}");
        }
    }
}
