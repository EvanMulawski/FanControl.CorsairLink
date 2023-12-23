using CorsairLink.Devices.CommanderCore;

namespace CorsairLink.Tests.CommanderCore;

public class CommanderCoreDataWriterTests
{
    [Fact]
    public void CreateSoftwareSpeedFixedPercentData_ReturnsSingleZeroByte_WhenChannelSpeedsEmpty()
    {
        // Arrange
        var channelSpeeds = new Dictionary<int, byte>
        {

        };

        // Act
        var data = CommanderCoreDataWriter.CreateSoftwareSpeedFixedPercentData(channelSpeeds);

        // Assert
        Assert.Equal("00", data.ToHexString());
    }

    [Fact]
    public void CreateSoftwareSpeedFixedPercentData_ReturnsExpectedBytes_WhenChannelSpeedsNotEmpty()
    {
        // Arrange
        var channelSpeeds = new Dictionary<int, byte>
        {
            { 0, 0x32 },
            { 1, 0x64 },
        };

        // Act
        var data = CommanderCoreDataWriter.CreateSoftwareSpeedFixedPercentData(channelSpeeds);

        // Assert
        Assert.Equal("020000320001006400", data.ToHexString());
    }

    [Fact]
    public void CreateCommandPacket_ReturnsExpectedBytes()
    {
        // Arrange
        var bufferSize = 64;
        byte[] command = [0xEE, 0xFF];
        byte[] data = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06];

        // Act
        var packet = CommanderCoreDataWriter.CreateCommandPacket(bufferSize, command, data);
        var packetCommand = packet.AsSpan(2, command.Length).ToArray();
        var packetData = packet.AsSpan(2 + command.Length, data.Length).ToArray();

        // Assert
        Assert.Equal(bufferSize, packet.Length);
        Assert.Equal(0x00, packet[0]);
        Assert.Equal(0x08, packet[1]);
        Assert.Equal(command, packetCommand);
        Assert.Equal(data, packetData);
    }
}
