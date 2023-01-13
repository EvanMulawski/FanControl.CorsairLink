namespace CorsairLink
{
    public class SpeedSensorReport
    {
        public SpeedSensorReport(IEnumerable<SpeedSensorData> speeds)
        {
            Speeds = speeds.ToList();
        }

        public IReadOnlyCollection<SpeedSensorData> Speeds { get; }
    }
}