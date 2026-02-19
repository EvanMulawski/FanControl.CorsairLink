using CorsairLink.Devices.CommanderCore;

namespace CorsairLink.Tests.CommanderCore;

public class CommanderCoreDataReaderTests
{
    [Fact]
    public void GetFirmwareVersion_ReturnsHumanReadableVersionString()
    {
        // Arrange
        var data = TestUtils.ParseHexString("000200020BDD0000000100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");

        // Act
        var firmwareVersion = CommanderCoreDataReader.GetFirmwareVersion(data);

        // Assert
        Assert.Equal("2.11.221", firmwareVersion);
    }

    [Fact]
    public void GetSpeedSensors_ReturnsAllSpeedSensors()
    {
        // Arrange
        var connectedSpeedsData = TestUtils.ParseHexString("000800090007070707070701010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");
        var speedsData = TestUtils.ParseHexString("000800060007940AD703BC03BB03C0030000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");

        // Act
        var sensors = CommanderCoreDataReader.GetSpeedSensors(connectedSpeedsData, speedsData);

        // Assert
        Assert.Equal(7, sensors.Count);
        Assert.Equal(5, sensors.ElementAt(5).Channel);
        Assert.False(sensors.ElementAt(5).IsConnected);
        Assert.Equal(default, sensors.ElementAt(5).Rpm);
        Assert.Equal(0, sensors.ElementAt(0).Channel);
        Assert.True(sensors.ElementAt(0).IsConnected);
        Assert.Equal(2708, sensors.ElementAt(0).Rpm);
    }

    [Fact]
    public void GetTemperatureSensors_ReturnsAllTemperatureSensors()
    {
        // Arrange
        var data = TestUtils.ParseHexString("000800100002000602010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");

        // Act
        var sensors = CommanderCoreDataReader.GetTemperatureSensors(data);

        // Assert
        Assert.Equal(2, sensors.Count);
        Assert.Equal(1, sensors.ElementAt(1).Channel);
        Assert.False(sensors.ElementAt(1).IsConnected);
        Assert.Equal(default, sensors.ElementAt(1).TempCelsius);
        Assert.Equal(0, sensors.ElementAt(0).Channel);
        Assert.True(sensors.ElementAt(0).IsConnected);
        Assert.Equal(51.8f, sensors.ElementAt(0).TempCelsius!.Value, 0.1f);
    }

    [Fact]
    public void GetSpeedSensors_ReturnsAllSpeedSensorsForCommanderDuoArgbMode()
    {
        // Arrange
        var connectedSpeedsData = TestUtils.ParseHexString("00080009000203030000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");
        var speedsData = TestUtils.ParseHexString("00080006000264046704000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");

        // Act
        var sensors = CommanderCoreDataReader.GetSpeedSensors(connectedSpeedsData, speedsData);

        // Assert
        Assert.Equal(2, sensors.Count);
        Assert.Equal(0, sensors.ElementAt(0).Channel);
        Assert.True(sensors.ElementAt(0).IsConnected);
        Assert.Equal(1124, sensors.ElementAt(0).Rpm);
        Assert.Equal(1, sensors.ElementAt(1).Channel);
        Assert.True(sensors.ElementAt(1).IsConnected);
        Assert.Equal(1127, sensors.ElementAt(1).Rpm);
    }
}
