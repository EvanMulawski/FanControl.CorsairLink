namespace CorsairLink.FlexUsb;

public enum ActionCode : byte
{
    ReadVersion = 0x00,
    ReadMemory = 0x08,
    WriteSMBusSettings = 0x11,
    ReadSMBusCommand = 0x12,
    WriteSMBusCommand = 0x13,
}
