namespace CorsairLink;

public class TemperatureSensor
{
    public TemperatureSensor(string name, int channel, int? tempCelsius)
    {
        Name = name;
        Channel = channel;
        TemperatureCelsius = tempCelsius;
    }

    public string Name { get; }
    public int Channel { get; }
    public int? TemperatureCelsius { get; set; }
}