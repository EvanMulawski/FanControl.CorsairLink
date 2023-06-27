namespace CorsairLink.SiUsbXpress;

public static class AckParser
{
    public static AckStatus Parse(byte[] data)
    {
        if (data == null)
            return AckStatus.Error;

        if (data.Length == 0)
            return AckStatus.Ok;

        return data[0] switch
        {
            (byte)AckStatus.Ok => AckStatus.Ok,
            (byte)AckStatus.Busy => AckStatus.Busy,
            _ => AckStatus.Error
        };
    }
}
