namespace CorsairLink
{
    public interface ITemperatureSensorReader
    {
        TemperatureSensorConfiguration GetTemperatureSensorConfiguration();
        int GetTemperatureSensorValue(int channelId);
    }
}