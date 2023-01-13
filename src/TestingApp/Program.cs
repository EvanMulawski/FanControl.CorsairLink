using CorsairLink;

var devices = DeviceManager.GetSupportedDevices();

foreach (var device in devices)
{
    device.Connect();

    if (!device.IsConnected)
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

    Console.WriteLine("------------------------------");
}
