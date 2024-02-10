namespace CorsairLink.Devices.ICueLink;

public static class KnownLinkDevices
{
    private static readonly List<KnownLinkDevice> _devices = [];
    private static readonly Dictionary<LinkDeviceType, Dictionary<byte, KnownLinkDevice>> _deviceLookup;

    static KnownLinkDevices()
    {
        _devices.Add(new KnownLinkDevice(LinkDeviceType.Fan, 0x00, "QX Fan", LinkDeviceFlags.All));
        _devices.Add(new KnownLinkDevice(LinkDeviceType.LiquidCooler, 0x00, "H100i", LinkDeviceFlags.All)); // black
        _devices.Add(new KnownLinkDevice(LinkDeviceType.LiquidCooler, 0x01, "H115i", LinkDeviceFlags.All)); // black
        _devices.Add(new KnownLinkDevice(LinkDeviceType.LiquidCooler, 0x02, "H150i", LinkDeviceFlags.All)); // black
        _devices.Add(new KnownLinkDevice(LinkDeviceType.LiquidCooler, 0x03, "H170i", LinkDeviceFlags.All)); // black
        _devices.Add(new KnownLinkDevice(LinkDeviceType.LiquidCooler, 0x04, "H100i", LinkDeviceFlags.All)); // white
        _devices.Add(new KnownLinkDevice(LinkDeviceType.LiquidCooler, 0x05, "H150i", LinkDeviceFlags.All)); // white
        _devices.Add(new KnownLinkDevice(LinkDeviceType.WaterBlock, 0x00, "XC7", LinkDeviceFlags.ReportsTemperature)); // stealth gray
        _devices.Add(new KnownLinkDevice(LinkDeviceType.WaterBlock, 0x01, "XC7", LinkDeviceFlags.ReportsTemperature)); // white

        _deviceLookup = InitializeDeviceLookup();
    }

    public static KnownLinkDevice? Find(LinkDeviceType type, byte model)
    {
        return _deviceLookup.TryGetValue(type, out var models)
            ? models.TryGetValue(model, out var knownLinkDevice)
                ? knownLinkDevice
                : default
            : default;
    }

    private static Dictionary<LinkDeviceType, Dictionary<byte, KnownLinkDevice>> InitializeDeviceLookup()
    {
        var lookup = new Dictionary<LinkDeviceType, Dictionary<byte, KnownLinkDevice>>();

        foreach (var device in _devices)
        {
            if (!lookup.ContainsKey(device.Type))
            {
                lookup[device.Type] = [];
            }

            lookup[device.Type][device.Model] = device;
        }

        return lookup;
    }
}
