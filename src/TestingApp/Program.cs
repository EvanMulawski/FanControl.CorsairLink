using CorsairLink;
using Device.Net;
using Hid.Net.Windows;
using Microsoft.Extensions.Logging;
using Usb.Net.Windows;

var loggerFactory = LoggerFactory
    .Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Error));

var hidFactory = new FilterDeviceDefinition(vendorId: HardwareIds.CorsairVendorId)
    .CreateWindowsHidDeviceFactory(loggerFactory);

var usbFactory = new FilterDeviceDefinition(vendorId: HardwareIds.CorsairVendorId)
    .CreateWindowsUsbDeviceFactory(loggerFactory);

var factories = hidFactory.Aggregate(usbFactory);
var allCorsairDeviceDefinitions = await factories.GetConnectedDeviceDefinitionsAsync();
var supportedDeviceDefinitions = allCorsairDeviceDefinitions.Where(x => x.ProductId.HasValue && HardwareIds.SupportedProductIds.Contains(x.ProductId.Value));

var devices = new List<CommanderProDevice>();

foreach (var deviceDefinition in supportedDeviceDefinitions)
{
    var device = await hidFactory.GetDeviceAsync(deviceDefinition);
    await device.InitializeAsync();
    var clcpDevice = new CommanderProDevice(device);
    devices.Add(clcpDevice);
}

var iter = 0;

while (true)
{
    foreach (var device in devices)
    {
        var fwVersion = await device.GetFirmwareVersionAsync();
        Console.WriteLine($"Firmware Version: {fwVersion}");

        for (var i = 0; i < 6; i++)
        {
            var rpm = await device.GetFanRpmAsync(i);
            Console.WriteLine($"Fan #{i + 1} RPM: {rpm}");
        }

        Console.WriteLine("====================");
        Console.WriteLine();
    }

    await Task.Delay(1000);

    ++iter;

    if (iter == 5)
    {
        Console.WriteLine("Setting fan 6 speed to 0rpm...");
        await devices[0].SetFanRpmAsync(5, 0);
        Console.WriteLine("====================");
        Console.WriteLine();
    }

    if (iter == 15)
    {
        Console.WriteLine("Setting fan 6 speed to 600rpm...");
        await devices[0].SetFanRpmAsync(5, 600);
        Console.WriteLine("====================");
        Console.WriteLine();
    }
}

//Console.ReadLine();
