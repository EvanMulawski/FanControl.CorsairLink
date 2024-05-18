using CorsairLink.Devices.HydroPlatinum;

namespace CorsairLink.Tests.HydroPlatinum
{
    public class HydroPlatinumDataWriterTests
    {
        [Theory]
        [InlineData("FFE81120008C39BF1913039EE803333802039EE80333360202FF0000FF800A01000000000000039E0000334A0200000000000000000000000000000000000000", 0x56)]
        [InlineData("3F88FF00FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", 0xE8)]
        public void CalculateChecksumByte_ShouldReturnExpectedChecksum(string packetString, byte expectedChecksum)
        {
            // Arrange
            var packet = TestUtils.ParseHexString(packetString);
            var sut = new HydroPlatinumDataWriter();

            // Act
            var result = sut.CalculateChecksumByte(packet);

            // Assert
            Assert.Equal(expectedChecksum, result);
        }

        [Fact]
        public void CreateCommandPacket_ShouldReturnExpectedPacket()
        {
            // Arrange
            byte command = 0xaa;
            byte sequenceNumber = 0x08;
            byte[] data = [0x01, 0x02, 0x03];
            var sut = new HydroPlatinumDataWriter();

            // Act
            var result = sut.CreateCommandPacket(0xaa, 0x08, data);
            var checksumByte = sut.CalculateChecksumByte(result);

            // Assert
            Assert.Equal(HydroPlatinumDataWriter.PAYLOAD_LENGTH, result[0]);
            Assert.Equal(command | sequenceNumber, result[1]);
            Assert.Equal(data[0], result[HydroPlatinumDataWriter.PAYLOAD_DATA_START_IDX]);
            Assert.Equal(data[1], result[HydroPlatinumDataWriter.PAYLOAD_DATA_START_IDX + 1]);
            Assert.Equal(data[2], result[HydroPlatinumDataWriter.PAYLOAD_DATA_START_IDX + 2]);
            Assert.Equal(checksumByte, result[HydroPlatinumDataWriter.PACKET_SIZE - 1]);
        }

        [Fact]
        public void CreateDefaultLightingColorData_ShouldReturnExpectedColorData()
        {
            // Arrange
            byte colorR = 64, colorG = 65, colorB = 66;
            var sut = new HydroPlatinumDataWriter();

            // Act
            var result = sut.CreateDefaultLightingColorData(colorR, colorG, colorB);

            // Assert
            for (int i = 0; i < result.Length; i++)
            {
                var c = i % 3;
                switch (c)
                {
                    case 0:
                        Assert.Equal(colorB, result[c]);
                        break;
                    case 1:
                        Assert.Equal(colorG, result[c]);
                        break;
                    case 2:
                        Assert.Equal(colorR, result[c]);
                        break;
                }
            }
        }
    }
}
