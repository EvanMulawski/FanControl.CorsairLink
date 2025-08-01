﻿namespace CorsairLink.Devices.ICueLink;

public static class KnownLinkDevices
{
    private static readonly List<KnownLinkDevice> _devices = [];
    private static readonly Dictionary<LinkDeviceModel, Dictionary<byte, KnownLinkDevice>> _deviceLookup;

    static KnownLinkDevices()
    {
        _devices.Add(new KnownLinkDevice(LinkDeviceModel.FanQxSeries, 0x00, "QX Fan", LinkDeviceFlags.All));
        _devices.Add(new KnownLinkDevice(LinkDeviceModel.LiquidCoolerHSeries, 0x00, "H100i", LinkDeviceFlags.All)); // black
        _devices.Add(new KnownLinkDevice(LinkDeviceModel.LiquidCoolerHSeries, 0x01, "H115i", LinkDeviceFlags.All)); // black
        _devices.Add(new KnownLinkDevice(LinkDeviceModel.LiquidCoolerHSeries, 0x02, "H150i", LinkDeviceFlags.All)); // black
        _devices.Add(new KnownLinkDevice(LinkDeviceModel.LiquidCoolerHSeries, 0x03, "H170i", LinkDeviceFlags.All)); // black
        _devices.Add(new KnownLinkDevice(LinkDeviceModel.LiquidCoolerHSeries, 0x04, "H100i", LinkDeviceFlags.All)); // white
        _devices.Add(new KnownLinkDevice(LinkDeviceModel.LiquidCoolerHSeries, 0x05, "H150i", LinkDeviceFlags.All)); // white
        _devices.Add(new KnownLinkDevice(LinkDeviceModel.WaterBlockXc7Series, 0x00, "XC7", LinkDeviceFlags.ReportsTemperature)); // stealth gray
        _devices.Add(new KnownLinkDevice(LinkDeviceModel.WaterBlockXc7Series, 0x01, "XC7", LinkDeviceFlags.ReportsTemperature)); // white
        _devices.Add(new KnownLinkDevice(LinkDeviceModel.WaterBlockXg3Series, 0x00, "XG3", LinkDeviceFlags.All));
        _devices.Add(new KnownLinkDevice(LinkDeviceModel.FanRxSeries, 0x00, "RX Fan", LinkDeviceFlags.ControlsSpeed | LinkDeviceFlags.ReportsSpeed));
        _devices.Add(new KnownLinkDevice(LinkDeviceModel.FanRxRgbSeries, 0x00, "RX RGB Fan", LinkDeviceFlags.ControlsSpeed | LinkDeviceFlags.ReportsSpeed));
        _devices.Add(new KnownLinkDevice(LinkDeviceModel.FanRxMaxSeries, 0x00, "RX MAX Fan", LinkDeviceFlags.All));
        _devices.Add(new KnownLinkDevice(LinkDeviceModel.FanRxMaxRgbSeries, 0x00, "RX MAX RGB Fan", LinkDeviceFlags.ControlsSpeed | LinkDeviceFlags.ReportsSpeed));
        _devices.Add(new KnownLinkDevice(LinkDeviceModel.PumpXd5Series, 0x00, "XD5", LinkDeviceFlags.All)); // stealth gray
        _devices.Add(new KnownLinkDevice(LinkDeviceModel.PumpXd5Series, 0x01, "XD5", LinkDeviceFlags.All)); // white
        _devices.Add(new KnownLinkDevice(LinkDeviceModel.FanLxSeries, 0x00, "LX Fan", LinkDeviceFlags.ControlsSpeed | LinkDeviceFlags.ReportsSpeed));
        _devices.Add(new KnownLinkDevice(LinkDeviceModel.LiquidCoolerTitanSeries, 0x00, "TITAN AIO", LinkDeviceFlags.All)); // model/color tbd
        _devices.Add(new KnownLinkDevice(LinkDeviceModel.LiquidCoolerTitanSeries, 0x01, "TITAN AIO", LinkDeviceFlags.All)); // model/color tbd
        _devices.Add(new KnownLinkDevice(LinkDeviceModel.LiquidCoolerTitanSeries, 0x02, "TITAN AIO", LinkDeviceFlags.All)); // model/color tbd
        _devices.Add(new KnownLinkDevice(LinkDeviceModel.LiquidCoolerTitanSeries, 0x03, "TITAN AIO", LinkDeviceFlags.All)); // model/color tbd
        _devices.Add(new KnownLinkDevice(LinkDeviceModel.LiquidCoolerTitanSeries, 0x04, "TITAN AIO", LinkDeviceFlags.All)); // model/color tbd
        _devices.Add(new KnownLinkDevice(LinkDeviceModel.LiquidCoolerTitanSeries, 0x05, "TITAN 360 RX RGB AIO", LinkDeviceFlags.All)); // white
        _devices.Add(new KnownLinkDevice(LinkDeviceModel.CapSwapModuleVrmFan, 0x00, "VRM Fan CapSwap Module", LinkDeviceFlags.ControlsSpeed | LinkDeviceFlags.ReportsSpeed));
        _devices.Add(new KnownLinkDevice(LinkDeviceModel.PumpXd6Series, 0x00, "XD6", LinkDeviceFlags.All)); // stealth gray
        _devices.Add(new KnownLinkDevice(LinkDeviceModel.PumpXd6Series, 0x01, "XD6", LinkDeviceFlags.All)); // white

        _deviceLookup = InitializeDeviceLookup();
    }

    public static KnownLinkDevice? Find(LinkDeviceModel type, byte model)
    {
        return _deviceLookup.TryGetValue(type, out var models)
            ? models.TryGetValue(model, out var knownLinkDevice)
                ? knownLinkDevice
                : default
            : default;
    }

    private static Dictionary<LinkDeviceModel, Dictionary<byte, KnownLinkDevice>> InitializeDeviceLookup()
    {
        var lookup = new Dictionary<LinkDeviceModel, Dictionary<byte, KnownLinkDevice>>();

        foreach (var device in _devices)
        {
            if (!lookup.ContainsKey(device.Model))
            {
                lookup[device.Model] = [];
            }

            lookup[device.Model][device.Variant] = device;
        }

        return lookup;
    }
}
