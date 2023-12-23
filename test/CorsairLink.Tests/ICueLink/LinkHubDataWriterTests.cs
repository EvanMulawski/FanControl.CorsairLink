using CorsairLink.Devices.ICueLink;

namespace CorsairLink.Tests.ICueLink;

public class LinkHubDataWriterTests
{
    [Fact]
    public void CreateSoftwareSpeedFixedPercentData_ReturnsSingleZeroByte_WhenChannelSpeedsEmpty()
    {
        var channelSpeeds = new Dictionary<int, byte>
        {

        };

        var data = LinkHubDataWriter.CreateSoftwareSpeedFixedPercentData(channelSpeeds);
        Assert.Equal("00", data.ToHexString());
    }

    [Fact]
    public void CreateSoftwareSpeedFixedPercentData_ReturnsExpectedBytes_WhenChannelSpeedsNotEmpty()
    {
        var channelSpeeds = new Dictionary<int, byte>
        {
            { 1, 0x32 },
            { 8, 0x64 },
        };

        var data = LinkHubDataWriter.CreateSoftwareSpeedFixedPercentData(channelSpeeds);
        Assert.Equal("020100320008006400", data.ToHexString());
    }

    [Fact]
    public void CreateCommandPacket_ReturnsExpectedBytes()
    {
        // Arrange
        var bufferSize = 64;
        byte[] command = [0xEE, 0xFF];
        byte[] data = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06];

        // Act
        var packet = LinkHubDataWriter.CreateCommandPacket(bufferSize, command, data);
        var packetCommand = packet.AsSpan(3, command.Length).ToArray();
        var packetData = packet.AsSpan(3 + command.Length, data.Length).ToArray();

        // Assert
        Assert.Equal(bufferSize, packet.Length);
        Assert.Equal(0x00, packet[0]);
        Assert.Equal(0x00, packet[1]);
        Assert.Equal(0x01, packet[2]);
        Assert.Equal(command, packetCommand);
        Assert.Equal(data, packetData);
    }
}
