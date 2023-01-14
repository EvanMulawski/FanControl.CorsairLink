namespace CorsairLink;

public class SpeedSensorReport
{
    public SpeedSensorReport(IEnumerable<SpeedSensor> speeds)
    {
        Speeds = speeds.ToList();
    }

    public IReadOnlyCollection<SpeedSensor> Speeds { get; }
}