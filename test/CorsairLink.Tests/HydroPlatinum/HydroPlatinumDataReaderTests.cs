using CorsairLink.Devices.HydroPlatinum;

namespace CorsairLink.Tests.HydroPlatinum;

public class HydroPlatinumDataReaderTests
{
    [Fact]
    public void GetState_ReturnsExpectedState_WhenDeviceHasThreeFans()
    {
        // Arrange
        var data = TestUtils.ParseHexString("FFE81120008C39BF1913039EE803333802039EE80333360202FF0000FF800A01000000000000039E0000334A0200000000000000000000000000000000000056");
        var sut = new HydroPlatinumDataReader(fanCount: 3);

        // Act
        var state = sut.GetState(data);

        // Assert
        Assert.Equal(0xE8, state.SequenceNumber);
        Assert.Equal(DeviceStatus.Success, state.Status);
        Assert.Equal(1, state.FirmwareVersionMajor);
        Assert.Equal(1, state.FirmwareVersionMinor);
        Assert.Equal(32, state.FirmwareVersionRevision);
        Assert.Equal(3, state.FanRpm.Length);
        Assert.Equal(568, state.FanRpm[0]);
        Assert.Equal(566, state.FanRpm[1]);
        Assert.Equal(586, state.FanRpm[2]);
        Assert.Equal(PumpMode.Performance, state.PumpMode);
        Assert.Equal(2688, state.PumpRpm);
        Assert.Equal(25.7f, state.LiquidTempCelsius);
    }

    [Fact]
    public void GetState_ReturnsExpectedState_WhenDeviceHasTwoFans()
    {
        // Arrange
        var data = TestUtils.ParseHexString("FFE81120008C39BF1913039EE803333802039EE80333360202FF0000FF800A01000000000000039E000000000000000000000000000000000000000000000056");
        var sut = new HydroPlatinumDataReader(fanCount: 2);

        // Act
        var state = sut.GetState(data);

        // Assert
        Assert.Equal(0xE8, state.SequenceNumber);
        Assert.Equal(DeviceStatus.Success, state.Status);
        Assert.Equal(1, state.FirmwareVersionMajor);
        Assert.Equal(1, state.FirmwareVersionMinor);
        Assert.Equal(32, state.FirmwareVersionRevision);
        Assert.Equal(2, state.FanRpm.Length);
        Assert.Equal(568, state.FanRpm[0]);
        Assert.Equal(566, state.FanRpm[1]);
        Assert.Equal(PumpMode.Performance, state.PumpMode);
        Assert.Equal(2688, state.PumpRpm);
        Assert.Equal(25.7f, state.LiquidTempCelsius);
    }
}
