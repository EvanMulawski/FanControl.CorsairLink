using CorsairLink.Devices.ICueLink;

namespace CorsairLink.Tests;

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
}
