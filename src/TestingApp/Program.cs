using CorsairLink;
using CorsairLink.Synchronization;

var devices = DeviceManager.GetSupportedDevices(new CorsairDevicesGuardManager(), null);

var connectedDevices = new List<IDevice>();

foreach (var device in devices)
{
    if (!device.Connect())
    {
        Console.WriteLine($"Device '{device.UniqueId}' did not connect!");
        continue;
    }

    connectedDevices.Add(device);

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
}

for (var i = 0; i < 5; i++)
{
    await Task.Delay(1000);

    foreach (var device in connectedDevices)
    {
        device.Refresh();

        Console.WriteLine(device.Name);

        foreach (var temp in device.TemperatureSensors)
        {
            Console.WriteLine($"  {temp.Name} ({temp.Channel}): {(temp.TemperatureCelsius.HasValue ? temp.TemperatureCelsius + "°C" : "n/a")}");
        }

        foreach (var speed in device.SpeedSensors)
        {
            Console.WriteLine($"  {speed.Name} ({speed.Channel}): {(speed.Rpm.HasValue ? speed.Rpm + "rpm" : "n/a")}");
        }

        Console.WriteLine("------------------------------");
    }
}

foreach (var device in connectedDevices)
{
    device.Disconnect();
}
