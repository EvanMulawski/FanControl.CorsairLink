namespace CorsairLink
{
    public class TemperatureSensorReport
    {
        public TemperatureSensorReport(IEnumerable<TemperatureSensorData> temperatures)
        {
            Temperatures = temperatures.ToList();
        }

        public IReadOnlyCollection<TemperatureSensorData> Temperatures { get; }
    }
}