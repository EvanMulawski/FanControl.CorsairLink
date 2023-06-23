using CorsairLink.Devices;

namespace CorsairLink.Tests;

public class HydroDeviceTests
{
    private static readonly byte[] IncomingStatePacketBytes = new byte[] {
        0x00, 0xFF, 0xB0, 0x11, 0x1F, 0x00, 0x71, 0x02,
        0x50, 0x1F, 0x08, 0x02, 0xCC, 0xE8, 0x03, 0xCC,
        0x00, 0x00, 0x02, 0xCC, 0xE8, 0x03, 0xCC, 0xCF,
        0x04, 0x01, 0xBF, 0x00, 0x00, 0xBF, 0x20, 0x09,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x66,
    };

    [Fact]
    public void GetFirmwareVersion_ShouldReturnFormattedFirmwareVersion()
    {
        // Arrange
        var deviceProxy = new TestDeviceProxy(
            IncomingStatePacketBytes
        );
        var device = new HydroPlatinumDevice(deviceProxy, new TestGuardManager(), new HydroPlatinumDeviceOptions { FanChannelCount = 2 }, null);

        // Act
        var fwVersion = device.GetFirmwareVersion();

        // Assert
        Assert.Equal("1.1.31", fwVersion);
    }

    [Fact]
    public void ReadState_ShouldReturnExpectedStateValues()
    {
        // Arrange
        var deviceProxy = new TestDeviceProxy();
        var device = new HydroPlatinumDevice(deviceProxy, new TestGuardManager(), new HydroPlatinumDeviceOptions { FanChannelCount = 2 }, null);

        // Act
        var state = device.ReadState(IncomingStatePacketBytes.AsSpan(1));

        // Assert
        Assert.Equal(1, state.FirmwareVersionMajor);
        Assert.Equal(1, state.FirmwareVersionMinor);
        Assert.Equal(31, state.FirmwareVersionRevision);
        Assert.Equal(31.3, state.LiquidTempCelsius, 0.00001);
        Assert.Equal(HydroPlatinumDevice.PumpMode.Balanced, state.PumpMode);
        Assert.Equal(2336, state.PumpRpm);
        Assert.Equal(0, state.FanRpm[0]);
        Assert.Equal(1231, state.FanRpm[1]);
    }

    [Fact]
    public void GenerateChecksum_ShouldReturnExpectedCrc8Byte()
    {
        // Arrange
        byte[] readBytes = new byte[] {
            0x00, 0xFF, 0xB0, 0x11, 0x1F, 0x00, 0x16, 0x00,
            0x31, 0x24, 0x00, 0x00, 0xFF, 0xE8, 0x03, 0xFF,
            0x00, 0x00, 0x00, 0xFF, 0xE8, 0x03, 0xFF, 0x00,
            0x00, 0x00, 0x8C, 0x00, 0x00, 0x8C, 0x98, 0x07,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x42,
        };
        var bytesForChecksum = readBytes.AsSpan(2, readBytes.Length - 3);

        // Act
        var result = HydroPlatinumDevice.GenerateChecksum(bytesForChecksum);

        // Assert
        Assert.Equal(readBytes.Last(), result);
    }

    [Fact]
    public void CreateCommand_ShouldGenerateExpectedCoolingPayload()
    {
        // Arrange
        const int FAN_0_POWER = 37;
        const int FAN_1_POWER = 85;
        var PUMP_POWER = Utils.ToFractionalByte(68);
        var coolingData = HydroPlatinumDevice.CreateCoolingCommandData(FAN_0_POWER, FAN_1_POWER, PUMP_POWER);
        var coolingCommand = HydroPlatinumDevice.CreateCoolingCommand(coolingData);
        byte sequenceNumber = 0x00;

        // Act
        var result = HydroPlatinumDevice.CreateCommand(HydroPlatinumDevice.Commands.Cooling, sequenceNumber, coolingCommand);

        // Assert
        Assert.Equal(0x00, result[0]);
        Assert.Equal(0x3f, result[1]);
        Assert.Equal(sequenceNumber | HydroPlatinumDevice.Commands.Cooling, result[2]);
        Assert.Equal(0x14, result[3]);
        Assert.Equal(0x00, result[4]);
        Assert.Equal(0xff, result[5]);
        Assert.Equal(0x05, result[6]);
        Assert.Equal(0xff, result[7]);
        Assert.Equal(0xff, result[8]);
        Assert.Equal(0xff, result[9]);
        Assert.Equal(0xff, result[10]);
        Assert.Equal(0xff, result[11]);
        Assert.Equal(0x03, result[12]); // fan 0 mode
        Assert.Equal(0xff, result[13]);
        Assert.Equal(0xff, result[14]);
        Assert.Equal(0xff, result[15]);
        Assert.Equal(0xff, result[16]);
        Assert.Equal(FAN_0_POWER, result[17]); // fan 0 power
        Assert.Equal(0x03, result[18]); // fan 1 mode
        Assert.Equal(0xff, result[19]);
        Assert.Equal(0xff, result[20]);
        Assert.Equal(0xff, result[21]);
        Assert.Equal(0xff, result[22]);
        Assert.Equal(FAN_1_POWER, result[23]); // fan 1 power
        Assert.Equal((byte)HydroPlatinumDevice.PumpMode.Performance, result[24]); // pump mode
        Assert.Equal(0xff, result[25]);
        Assert.Equal(0xff, result[26]);
        Assert.Equal(0xff, result[27]);
        Assert.Equal(0xff, result[28]);
        Assert.Equal(0xff, result[29]);
        Assert.Equal(0xff, result[30]);
        Assert.Equal(0xff, result[31]);
        Assert.Equal(0xff, result[32]);
        Assert.Equal(0xff, result[33]);
        Assert.Equal(0xff, result[34]);
        Assert.Equal(0xff, result[35]);
        Assert.Equal(0xff, result[36]);
        Assert.Equal(0xff, result[37]);
        Assert.Equal(0xff, result[38]);
        Assert.Equal(0xff, result[39]);
        Assert.Equal(0xff, result[40]);
        Assert.Equal(0xff, result[41]);
        Assert.Equal(0xff, result[42]);
        Assert.Equal(0xff, result[43]);
        Assert.Equal(0xff, result[44]);
        Assert.Equal(0xff, result[45]);
        Assert.Equal(0xff, result[46]);
        Assert.Equal(0xff, result[47]);
        Assert.Equal(0xff, result[48]);
        Assert.Equal(0xff, result[49]);
        Assert.Equal(0xff, result[50]);
        Assert.Equal(0xff, result[51]);
        Assert.Equal(0xff, result[52]);
        Assert.Equal(0xff, result[53]);
        Assert.Equal(0xff, result[54]);
        Assert.Equal(0xff, result[55]);
        Assert.Equal(0xff, result[56]);
        Assert.Equal(0xff, result[57]);
        Assert.Equal(0xff, result[58]);
        Assert.Equal(0xff, result[59]);
        Assert.Equal(0xff, result[60]);
        Assert.Equal(0xff, result[61]);
        Assert.Equal(0xff, result[62]);
        Assert.Equal(0xff, result[63]);
        Assert.NotEqual(0xff, result[64]); // CRC
    }

    [Theory]
    [InlineData(0, HydroPlatinumDevice.PumpMode.Quiet)]
    [InlineData(33, HydroPlatinumDevice.PumpMode.Quiet)]
    [InlineData(34, HydroPlatinumDevice.PumpMode.Balanced)]
    [InlineData(67, HydroPlatinumDevice.PumpMode.Balanced)]
    [InlineData(68, HydroPlatinumDevice.PumpMode.Performance)]
    [InlineData(100, HydroPlatinumDevice.PumpMode.Performance)]
    internal void GetPumpMode_ShouldReturnExpectedPumpMode(int requestedPowerPercent, HydroPlatinumDevice.PumpMode expectedPumpMode)
    {
        // Arrange
        var requestedPower = Utils.ToFractionalByte(requestedPowerPercent);

        // Act
        var result = HydroPlatinumDevice.GetPumpMode(requestedPower);

        // Assert
        Assert.Equal(expectedPumpMode, result);
    }
}