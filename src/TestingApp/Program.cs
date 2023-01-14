using CorsairLink;

var devices = DeviceManager.GetSupportedDevices();

foreach (var device in devices)
{
    if (!device.Connect())
    {
        Console.WriteLine($"Device '{device.DevicePath}' did not connect!");
        continue;
    }

    Console.WriteLine(device.Name);
    Console.WriteLine($"  Device path: {device.DevicePath}");

    if (device is IReportFirmwareVersion fw)
    {
        Console.WriteLine($"  FW ver: {fw.GetFirmwareVersion()}");
    }

    if (device is IReportTemperatureSensors temps)
    {
        var tempsReport = temps.GetTemperatures();

        foreach (var temp in tempsReport.Temperatures)
        {
            Console.WriteLine($"  {temp.Name} ({temp.Channel}): {(temp.TemperatureCelsius.HasValue ? temp.TemperatureCelsius + "°C" : "n/a")}");
        }
    }

    if (device is IReportSpeedSensors speeds)
    {
        var speedsReport = speeds.GetSpeeds();

        foreach (var speed in speedsReport.Speeds)
        {
            Console.WriteLine($"  {speed.Name} ({speed.Channel}): {(speed.Rpm.HasValue ? speed.Rpm + "rpm" : "n/a")}");
        }
    }

    Console.WriteLine("------------------------------");
}
