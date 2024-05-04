using CorsairLink.Devices.HydroPlatinum;
using System.Text;

namespace CorsairLink.Devices;

public class HydroPlatinumDeviceState
{
    public byte SequenceNumber { get; set; }
    public DeviceStatus Status { get; set; }
    public int FirmwareVersionMajor { get; set; }
    public int FirmwareVersionMinor { get; set; }
    public int FirmwareVersionRevision { get; set; }
    public int[] FanRpm { get; set; }
    public PumpMode PumpMode { get; set; }
    public int PumpRpm { get; set; }
    public float LiquidTempCelsius { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        for (var i = 0; i < FanRpm.Length; i++)
        {
            sb.AppendFormat("fan{0}Rpm={1}, ", i + 1, FanRpm[i]);
        }
        sb.AppendFormat("pumpMode={0}, ", PumpMode);
        sb.AppendFormat("pumpRpm={0}, ", PumpRpm);
        sb.AppendFormat("liquidTempCelsius={0}", LiquidTempCelsius);
        return sb.ToString();
    }
}
