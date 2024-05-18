using CorsairLink.Devices.HydroPlatinum;

namespace CorsairLink.Devices;

public class OneComputerDeviceOptions : HydroPlatinumDeviceOptions
{
    public new int FanChannelCount => OneComputerDataReader.FanChannelCount;
}
