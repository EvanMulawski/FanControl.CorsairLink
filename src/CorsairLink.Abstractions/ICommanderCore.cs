namespace CorsairLink;

public interface ICommanderCore : IDevice, IReportSpeedSensors, IReportTemperatureSensors, IReportFirmwareVersion, ISupportFixedPercentSpeedControl
{
}
