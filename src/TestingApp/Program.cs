using CorsairLink;

var devices = DeviceManager.GetSupportedDevices().CommanderProDevices;

foreach (var device in devices)
{
    device.Connect();

    if (!device.IsConnected)
    {
        Console.WriteLine($"Device '{device.DevicePath}' did not connect!");
    }
}

var iter = 0;

while (true)
{
    //var fanConfig = devices[0].GetFanConfiguration();
    //var tempConfig = devices[0].GetTemperatureSensorConfiguration();

    foreach (var device in devices)
    {
        //var fwVersion = device.GetFirmwareVersion();
        //Console.WriteLine($"Firmware Version: {fwVersion}");

        for (var i = 0; i < 6; i++)
        {
            var rpm = device.GetFanRpm(i);
            Console.WriteLine($"Fan #{i + 1} RPM: {rpm}");
        }

        Console.WriteLine("====================");
        Console.WriteLine();
    }

    await Task.Delay(1000);

    ++iter;

    if (iter == 5)
    {
        Console.WriteLine("Setting fan 6 speed to 0%...");
        devices[0].SetFanPower(5, 0);
        Console.WriteLine("====================");
        Console.WriteLine();
    }

    if (iter == 15)
    {
        Console.WriteLine("Setting fan 6 speed to 100%...");
        devices[0].SetFanPower(5, 100);
        Console.WriteLine("====================");
        Console.WriteLine();
    }

    if (iter == 25)
    {
        Console.WriteLine("Setting fan 6 speed to 600rpm...");
        devices[0].SetFanRpm(5, 600);
        Console.WriteLine("====================");
        Console.WriteLine();
    }
}

//Console.ReadLine();
