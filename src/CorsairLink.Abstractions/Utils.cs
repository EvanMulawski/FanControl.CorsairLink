namespace CorsairLink;

public static class Utils
{
    public static byte[] CreateRequest(byte command)
    {
        var writeBuf = new byte[64];
        writeBuf[1] = command;
        return writeBuf;
    }
}
