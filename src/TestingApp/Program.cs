using CorsairLink;

var devices = DeviceManager.GetSupportedDevices();

foreach (var device in devices)
{
    if (!device.Connect())
    {
        Console.WriteLine($"Device '{device.UniqueId}' did not connect!");
        continue;
    }

    Console.WriteLine(device.Name);
    Console.WriteLine($"  Device ID: {device.UniqueId}");
    Console.WriteLine($"  Firmware Version: {device.GetFirmwareVersion()}");

    foreach (var temp in device.TemperatureSensors)
    {
        Console.WriteLine($"  {temp.Name} ({temp.Channel}): {(temp.TemperatureCelsius.HasValue ? temp.TemperatureCelsius + "°C" : "n/a")}");
    }

    foreach (var speed in device.SpeedSensors)
    {
        Console.WriteLine($"  {speed.Name} ({speed.Channel}): {(speed.Rpm.HasValue ? speed.Rpm + "rpm" : "n/a")}");
    }

    Console.WriteLine("------------------------------");

    device.Disconnect();
}
