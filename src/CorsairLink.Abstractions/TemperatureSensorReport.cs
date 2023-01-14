namespace CorsairLink;

public class TemperatureSensorReport
{
    public TemperatureSensorReport(IEnumerable<TemperatureSensor> temperatures)
    {
        Temperatures = temperatures.ToList();
    }

    public IReadOnlyCollection<TemperatureSensor> Temperatures { get; }
}