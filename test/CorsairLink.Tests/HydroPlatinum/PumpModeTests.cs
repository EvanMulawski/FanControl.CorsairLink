using CorsairLink.Devices;
using CorsairLink.Devices.HydroPlatinum;

namespace CorsairLink.Tests.HydroPlatinum
{
    public class PumpModeTests
    {
        [Theory]
        [InlineData(0, PumpMode.Quiet)]
        [InlineData(33, PumpMode.Quiet)]
        [InlineData(34, PumpMode.Balanced)]
        [InlineData(67, PumpMode.Balanced)]
        [InlineData(68, PumpMode.Performance)]
        [InlineData(100, PumpMode.Performance)]
        public void GetPumpMode_ShouldReturnExpectedPumpMode(int requestedPowerPercent, PumpMode expectedPumpMode)
        {
            // Arrange
            var requestedPower = Utils.ToFractionalByte(requestedPowerPercent);

            // Act
            var result = HydroPlatinumDevice.GetPumpMode(requestedPower);

            // Assert
            Assert.Equal(expectedPumpMode, result);
        }
    }
}
