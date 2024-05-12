using System.Text;

namespace CorsairLink.Devices.HydroPlatinum;

public class OneComputerDeviceState : HydroPlatinumDeviceState
{
    public new int FanRpm { get; set; }
    public int GpuPumpRpm { get; set; }
    public float GpuLiquidTempCelsius { get; set; }

    public bool IsGpuUsingPump()
    {
        var isTempFail = Status == DeviceStatus.TempFail;
        var isGpuPumpSpeedZero = GpuPumpRpm == 0;
        return !isTempFail && !isGpuPumpSpeedZero;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendFormat("fanRpm={0}, ", FanRpm);
        sb.AppendFormat("cpuPumpMode={0}, ", PumpMode);
        sb.AppendFormat("cpuPumpRpm={0}, ", PumpRpm);
        sb.AppendFormat("cpuLiquidTempCelsius={0}", LiquidTempCelsius);
        sb.AppendFormat("gpuPumpRpm={0}, ", GpuPumpRpm);
        sb.AppendFormat("gpuLiquidTempCelsius={0}", GpuLiquidTempCelsius);
        return sb.ToString();
    }
}
