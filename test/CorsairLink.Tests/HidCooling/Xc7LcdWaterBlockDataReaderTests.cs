using CorsairLink.Devices.HidCooling;

namespace CorsairLink.Tests.HidCooling;

public class Xc7LcdWaterBlockDataReaderTests
{
    [Fact]
    public void GetFirmwareVersion_ReturnsVersionString()
    {
        // Arrange
        var data = TestUtils.ParseHexString("050ca9ce4062302e302e302e3139000000000000000000000000000000000000");

        // Act
        var firmwareVersion = Xc7LcdWaterBlockDataReader.GetFirmwareVersion(data);

        // Assert
        Assert.Equal("0.0.0.19", firmwareVersion);
    }

    [Fact]
    public void GetLiquidTemperature_ReturnsLiquidTemperatureValue()
    {
        // Arrange
        var data = TestUtils.ParseHexString("1800050100002602000058020000000000000000000000000000000000000000");

        // Act
        var liquidTemp = Xc7LcdWaterBlockDataReader.GetLiquidTemperature(data);

        // Assert
        Assert.Equal(26.1f, liquidTemp, 0.05f);
    }
}
