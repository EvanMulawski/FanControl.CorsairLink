namespace CorsairLink.Devices.CommanderCore;

public sealed class CommanderCoreSpeedSensor
{
    public CommanderCoreSpeedSensor(int channel, CommanderCoreSpeedSensorStatus status, int? rpm)
    {
        Channel = channel;
        Status = status;
        Rpm = rpm;
    }

    public int Channel { get; }
    public CommanderCoreSpeedSensorStatus Status { get; }
    public int? Rpm { get; }
    public bool IsConnected => Status.IsConnected();
}

public enum CommanderCoreSpeedSensorStatus : byte
{
    Available = 0x07,
    AvailableCommanderDuo = 0x03,
    Unavailable = 0x01,
}

public static class CommanderCoreSpeedSensorStatusExtensions
{
    public static bool IsConnected(this CommanderCoreSpeedSensorStatus status)
    {
        return status == CommanderCoreSpeedSensorStatus.Available
            || status == CommanderCoreSpeedSensorStatus.AvailableCommanderDuo;
    }
}