using CorsairLink;
using Device.Net;
using Hid.Net.Windows;
using Microsoft.Extensions.Logging;
using Usb.Net.Windows;

uint vendorId = 0x1b1c; // Corsair
uint productId = 0x1d00; // Obsidian 1000D Commander Pro

var loggerFactory = LoggerFactory
    .Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Error));

var hidFactory = new FilterDeviceDefinition(vendorId: vendorId, productId: productId)
    .CreateWindowsHidDeviceFactory(loggerFactory);

var usbFactory = new FilterDeviceDefinition(vendorId: vendorId, productId: productId)
    .CreateWindowsUsbDeviceFactory(loggerFactory);

var factories = hidFactory.Aggregate(usbFactory);

var deviceDefinitions = await factories.GetConnectedDeviceDefinitionsAsync();

while (true)
{
    foreach (var deviceDefinition in deviceDefinitions)
    {
        using var hidDevice = await hidFactory.GetDeviceAsync(deviceDefinition);
        var device = new CommanderProDevice(hidDevice);
        await device.InitializeDeviceAsync();
        var fwVersion = await device.GetFirmwareVersionAsync();

        Console.WriteLine(deviceDefinition.ProductName);
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
}

//Console.ReadLine();
