namespace CorsairLink;

public interface IDevice
{
    string UniqueId { get; }
    string Name { get; }
    IReadOnlyCollection<SpeedSensor> SpeedSensors { get; }
    IReadOnlyCollection<TemperatureSensor> TemperatureSensors { get; }

    bool Connect();
    void Disconnect();
    string GetFirmwareVersion();
    void Refresh();
    void SetChannelPower(int channel, int percent);
    void ResetChannel(int channel);
}
