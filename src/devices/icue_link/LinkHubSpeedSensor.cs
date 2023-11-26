namespace CorsairLink.Devices.ICueLink;

public sealed class LinkHubSpeedSensor
{
    public LinkHubSpeedSensor(int channel, LinkHubSpeedSensorStatus status, int? rpm)
    {
        Channel = channel;
        Status = status;
        Rpm = rpm;
    }

    public int Channel { get; }
    public LinkHubSpeedSensorStatus Status { get; }
    public int? Rpm { get; }
}

public enum LinkHubSpeedSensorStatus : byte
{
    Available = 0x00,
    Unavailable = 0x01,
}