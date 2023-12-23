using CorsairLink.Devices;

namespace CorsairLink.Tests.Coolit
{
    public class CoolitDeviceTests
    {
        [Theory]
        [InlineData(0xfb, 0x16, (short)22)]
        [InlineData(0x3d, 0xb2, default)]
        public void ParseTemperatureSensorValue_ShouldReturnCorrectTemperature_WhenDataBytesEqualPositiveNumber(byte byte3, byte byte4, short? expected)
        {
            // Arrange
            var data = new byte[] { 0x00, 0x00, 0x00, byte3, byte4 };

            // Act
            var result = CoolitDevice.ParseTemperatureSensorValue(data);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0x00, 0x00)]
        [InlineData(0x01, 0x02)]
        [InlineData(0x32, 0x7f)]
        [InlineData(0x64, 0xff)]
        [InlineData(0xff, 0xff)]
        public void CreateFanPowerValue_ShouldReturnExpectedValue(byte percent, byte expected)
        {
            // Act
            var result = CoolitDevice.CreateFanPowerValue(percent);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
