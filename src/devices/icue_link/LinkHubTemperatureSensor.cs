namespace CorsairLink.Devices.ICueLink;

public sealed class LinkHubTemperatureSensor
{
    public LinkHubTemperatureSensor(int channel, LinkHubTemperatureSensorStatus status, float? tempCelsius)
    {
        Channel = channel;
        Status = status;
        TempCelsius = tempCelsius;
    }

    public int Channel { get; }
    public LinkHubTemperatureSensorStatus Status { get; }
    public float? TempCelsius { get; }
}

public enum LinkHubTemperatureSensorStatus : byte
{
    Available = 0x00,
    Unavailable = 0x01,
}