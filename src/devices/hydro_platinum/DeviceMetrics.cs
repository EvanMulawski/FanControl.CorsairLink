using System.Diagnostics;

namespace CorsairLink.Devices.HydroPlatinum;

public sealed class DeviceMetrics
{
    private readonly RollingAverageCalculator _writeDelayRollingAverageCalculator;

    private long _writeStart;

    public DeviceMetrics(double defaultWriteDelayAverage)
    {
        _writeDelayRollingAverageCalculator = new(30, defaultWriteDelayAverage);
    }

    public void WriteStart()
    {
        _writeStart = Stopwatch.GetTimestamp();
    }

    public double WriteEnd()
    {
        var now = Stopwatch.GetTimestamp();
        var result = TimeSpan.FromTicks(now - _writeStart).TotalMilliseconds;
        return _writeDelayRollingAverageCalculator.Update(result);
    }
}
