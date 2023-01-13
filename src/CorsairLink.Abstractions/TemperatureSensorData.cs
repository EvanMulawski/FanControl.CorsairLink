namespace CorsairLink
{
    public class TemperatureSensorData
    {
        public TemperatureSensorData(string name, int channel, int? tempCelsius)
        {
            Name = name;
            Channel = channel;
            TemperatureCelsius = tempCelsius;
        }

        public string Name { get; }
        public int Channel { get; }
        public int? TemperatureCelsius { get; }
    }
}