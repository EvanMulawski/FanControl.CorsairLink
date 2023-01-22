namespace CorsairLink;

public static class Utils
{
    public static int Clamp(int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }
        else if (value > max)
        {
            return max;
        }

        return value;
    }

    private static readonly char[] HEX_CHARS = "0123456789ABCDEF".ToCharArray();

    public static string ToHexString(this ReadOnlySpan<byte> bytes)
    {
        char[] hexChars = new char[bytes.Length * 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            int v = bytes[i] & 0xff;
            hexChars[i * 2] = HEX_CHARS[v >> 4];
            hexChars[i * 2 + 1] = HEX_CHARS[v & 0x0f];
        }
        return new string(hexChars);
    }
}
