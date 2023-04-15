namespace CorsairLink;

public class SpeedSensor
{
    public SpeedSensor(string name, int channel, int? rpm, bool supportsControl)
    {
        Name = name;
        Channel = channel;
        Rpm = rpm;
        SupportsControl = supportsControl;
    }

    public string Name { get; }
    public int Channel { get; }
    public int? Rpm { get; set; }
    public bool SupportsControl { get; }

    public override string ToString() => $"{Name} (channel: {Channel}, controllable: {SupportsControl}): {Rpm} RPM";
}