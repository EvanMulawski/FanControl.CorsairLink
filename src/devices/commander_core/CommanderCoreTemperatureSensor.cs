namespace CorsairLink.Devices.CommanderCore;

public sealed class CommanderCoreTemperatureSensor
{
    public CommanderCoreTemperatureSensor(int channel, CommanderCoreTemperatureSensorStatus status, float? tempCelsius)
    {
        Channel = channel;
        Status = status;
        TempCelsius = tempCelsius;
    }

    public int Channel { get; }
    public CommanderCoreTemperatureSensorStatus Status { get; }
    public float? TempCelsius { get; }
    public bool IsConnected => Status.IsConnected();
}

public enum CommanderCoreTemperatureSensorStatus : byte
{
    Available = 0x00,
    Unavailable = 0x01,
}

public static class CommanderCoreTemperatureSensorStatusExtensions
{
    public static bool IsConnected(this CommanderCoreTemperatureSensorStatus status)
    {
        return status == CommanderCoreTemperatureSensorStatus.Available;
    }
}