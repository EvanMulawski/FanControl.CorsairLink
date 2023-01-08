namespace CorsairLink;

public interface ICommanderPro : IDevice
{
    string GetFirmwareVersion();

    int GetFanRpm(int channelId);

    void SetFanRpm(int channelId, int speedPercent);

    int GetTemperatureSensorValue(int channelId);

    FanConfiguration GetFanConfiguration();
    TemperatureSensorConfiguration GetTemperatureSensorConfiguration();
    void SetFanPower(int channelId, int percent);
}
