namespace CorsairLink;

public class SpeedSensor
{
    public SpeedSensor(string name, int channel, int? rpm)
    {
        Name = name;
        Channel = channel;
        Rpm = rpm;
    }

    public string Name { get; }
    public int Channel { get; }
    public int? Rpm { get; set; }
}