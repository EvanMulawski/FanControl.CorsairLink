namespace CorsairLink.SiUsbXpress;

public enum AckStatus : byte
{
    Ok = 0x00,
    Busy = 0x80,
    Error = 0xFF,
}
