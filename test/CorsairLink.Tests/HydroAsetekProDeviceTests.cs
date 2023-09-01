using CorsairLink.Devices;

namespace CorsairLink.Tests;

public class HydroAsetekProDeviceTests
{
    [Fact]
    public void GenerateChecksum_ShouldReturnExpectedCrc16Bytes()
    {
        // Arrange
        byte[] bytes = new byte[] {
            0x64, 0x64, 0x64, 0x64, 0x64, 0x64, 0x64, 0x64,
            0x64, 0x64, 0x64, 0x64, 0x64, 0x64, 0x64, 0x64,
            0x64, 0x64, 0x64,
        };

        // Act
        var result = HydroAsetekProDevice.GenerateChecksum(bytes);

        // Assert
        Assert.Equal(63835, result);
    }
}
