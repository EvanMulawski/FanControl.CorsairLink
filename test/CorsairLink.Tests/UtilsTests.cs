namespace CorsairLink.Tests;

public class UtilsTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(20, 51)]
    [InlineData(50, 128)]
    [InlineData(100, 255)]
    [InlineData(-1, 0)]
    [InlineData(256, 255)]
    public void ToFractionalByte_ReturnsExpectedValue(int intValue, byte expectedFractionalByteValue)
    {
        // Act
        var result = Utils.ToFractionalByte(intValue);

        // Assert
        Assert.Equal(expectedFractionalByteValue, result);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(128, 50)]
    [InlineData(255, 100)]
    public void FromFractionalByte_ReturnsExpectedValue(byte fractionalByteValue, byte expectedIntValue)
    {
        // Act
        var result = Utils.FromFractionalByte(fractionalByteValue);

        // Assert
        Assert.Equal(expectedIntValue, result);
    }
}
