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
}

public enum CommanderCoreSpeedSensorStatus : byte
{
    Available = 0x07,
    Unavailable = 0x01,
}