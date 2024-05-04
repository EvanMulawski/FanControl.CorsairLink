namespace CorsairLink.Devices.HydroPlatinum;

public enum DeviceStatus : byte
{
    Success = 0x00,
    TempFail = 0x01,
    PumpFail = 0x02,
    SavingSettings = 0x08,
    SequenceError = 0x10,
    CRCError = 0x20,
    CipherError = 0x30,
}
