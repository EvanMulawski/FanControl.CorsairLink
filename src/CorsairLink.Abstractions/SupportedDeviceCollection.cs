using System.Collections;

namespace CorsairLink;

public class SupportedDeviceCollection : IEnumerable<IDevice>
{
    public List<ICommanderPro> CommanderProDevices { get; } = new List<ICommanderPro>(1);

    private IEnumerator<IDevice> GetEnumeratorImpl()
    {
        return CommanderProDevices.GetEnumerator();
    }

    IEnumerator<IDevice> IEnumerable<IDevice>.GetEnumerator() => GetEnumeratorImpl();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumeratorImpl();
}
