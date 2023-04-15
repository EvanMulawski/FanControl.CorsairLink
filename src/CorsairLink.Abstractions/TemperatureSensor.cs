namespace CorsairLink;

public class TemperatureSensor
{
    public TemperatureSensor(string name, int channel, float? tempCelsius)
    {
        Name = name;
        Channel = channel;
        TemperatureCelsius = tempCelsius;
    }

    public string Name { get; }
    public int Channel { get; }
    public float? TemperatureCelsius { get; set; }

    public override string ToString() => $"{Name} (channel: {Channel}): {TemperatureCelsius} C";
}