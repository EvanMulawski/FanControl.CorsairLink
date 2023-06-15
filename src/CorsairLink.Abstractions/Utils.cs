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

    public static byte ToFractionalByte(int value) => (byte)((value * byte.MaxValue + 50) / 100);

    public static int FromFractionalByte(byte value) => (100 * value + byte.MaxValue / 2) / byte.MaxValue;

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

    public static string ToHexString(this byte[] bytes) => ToHexString(bytes.AsSpan());

    public static float FromLinear11(ReadOnlySpan<byte> bytes)
    {
        int value = bytes[1] << 8 | bytes[0];

        int mantissa = value & 0x7FF;
        if (mantissa > 1023)
            mantissa -= 2048;

        int exponent = value >> 11;
        if (exponent > 15)
            exponent -= 32;

        return mantissa * (float)Math.Pow(2, exponent);
    }

    public static bool GetEnvironmentFlag(string flagName)
    {
        var variableValue = Environment.GetEnvironmentVariable(flagName);
        if (string.IsNullOrEmpty(variableValue))
        {
            variableValue = Environment.GetEnvironmentVariable(flagName, EnvironmentVariableTarget.Machine);
        }
        return !string.IsNullOrEmpty(variableValue) && (variableValue.ToLower() == "true" || variableValue == "1");
    }
}
