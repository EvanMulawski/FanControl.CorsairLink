using System.Collections;

namespace CorsairLink;

public sealed class SupportedDeviceCollection : IEnumerable<IDevice>
{
    public List<IDevice> CommanderProDevices { get; } = new List<IDevice>(1);
    public List<IDevice> CommanderCoreDevices { get; } = new List<IDevice>(1);
    public List<IDevice> HydroDevices { get; } = new List<IDevice>(1);
    public List<IDevice> CoolitDevices { get; } = new List<IDevice>(1);

    private IEnumerator<IDevice> GetEnumeratorImpl()
    {
        return CommanderProDevices
            .Union(CommanderCoreDevices)
            .Union(HydroDevices)
            .Union(CoolitDevices)
            .GetEnumerator();
    }

    IEnumerator<IDevice> IEnumerable<IDevice>.GetEnumerator() => GetEnumeratorImpl();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumeratorImpl();
}
