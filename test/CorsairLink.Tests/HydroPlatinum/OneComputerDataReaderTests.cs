using CorsairLink.Devices.HydroPlatinum;

namespace CorsairLink.Tests.HydroPlatinum;

public class OneComputerDataReaderTests
{
    [Fact]
    public void GetState_ReturnsExpectedState()
    {
        // Arrange
        var data = TestUtils.ParseHexString("FF28100700C6482D23000800E8030000000800E80300940702FF0000FFE80A000000000000000000000000000000000000FB18000000000000000000000000DF");
        var sut = new OneComputerDataReader();

        // Act
        var state = (OneComputerDeviceState)sut.GetState(data);

        // Assert
        Assert.Equal(0x28, state.SequenceNumber);
        Assert.Equal(DeviceStatus.Success, state.Status);
        Assert.Equal(1, state.FirmwareVersionMajor);
        Assert.Equal(0, state.FirmwareVersionMinor);
        Assert.Equal(7, state.FirmwareVersionRevision);
        Assert.Equal(0, state.FanRpm);
        Assert.Equal(PumpMode.Performance, state.PumpMode);
        Assert.Equal(2792, state.PumpRpm);
        Assert.Equal(35.2f, state.LiquidTempCelsius);
        Assert.Equal(1940, state.GpuPumpRpm);
        Assert.Equal(25.0f, state.GpuLiquidTempCelsius);
    }
}
