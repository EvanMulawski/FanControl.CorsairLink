namespace CorsairLink;

public class RgbColor
{
    public RgbColor(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
    }

    public byte R { get; }
    public byte G { get; }
    public byte B { get; }

    public static bool TryParse(string? value, out RgbColor? color)
    {
        color = null;

        if (value is null)
            return false;

        string[] parts = value.Split(',');

        if (parts.Length != 3)
            return false;

        if (!byte.TryParse(parts[0], out byte r) ||
            !byte.TryParse(parts[1], out byte g) ||
            !byte.TryParse(parts[2], out byte b))
        {
            return false;
        }

        color = new RgbColor(r, g, b);

        return true;
    }
}
