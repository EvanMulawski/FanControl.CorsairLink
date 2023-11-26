namespace CorsairLink.Tests;

internal static class TestUtils
{
    public static byte[] ParseHexString(string hexString)
    {
        if (hexString.Length % 2 != 0)
        {
            throw new ArgumentException("Input string must have an even number of characters.");
        }

        byte[] bytes = new byte[hexString.Length / 2];
        for (int i = 0; i < hexString.Length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
        }

        return bytes;
    }
}
