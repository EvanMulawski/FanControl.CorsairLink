using System.Collections;

namespace CorsairLink;

public sealed class SupportedDeviceCollection : IEnumerable<IDevice2>
{
    public List<IDevice2> CommanderProDevices { get; } = new List<IDevice2>(1);
    public List<IDevice2> CommanderCoreDevices { get; } = new List<IDevice2>(1);

    private IEnumerator<IDevice2> GetEnumeratorImpl()
    {
        return CommanderProDevices.Union(CommanderCoreDevices).GetEnumerator();
    }

    IEnumerator<IDevice2> IEnumerable<IDevice2>.GetEnumerator() => GetEnumeratorImpl();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumeratorImpl();
}
