namespace CorsairLink;

public static class Utils
{
    public static Span<byte> CreateRequest(byte command, int length)
    {
        var writeBuf = new byte[length];
        writeBuf[1] = command;
        return writeBuf;
    }

    public static Span<byte> CreateResponse(int length)
    {
        return new byte[length];
    }
}
