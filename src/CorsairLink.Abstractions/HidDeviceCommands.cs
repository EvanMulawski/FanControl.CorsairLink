namespace CorsairLink;

public static class HidDeviceCommands
{
    public static class CommanderPro
    {
        public static readonly byte ReadStatus = 0x01;
        public static readonly byte ReadFirmwareVersion = 0x02;
        public static readonly byte ReadDeviceId = 0x03;
        public static readonly byte WriteDeviceId = 0x04;
        public static readonly byte StartFirmwareUpdate = 0x05;
        public static readonly byte ReadBootloaderVersion = 0x06;
        public static readonly byte WriteTestFlag = 0x07;
        public static readonly byte ReadTemperatureMask = 0x10;
        public static readonly byte ReadTemperatureValue = 0x11;
        public static readonly byte ReadVoltageValue = 0x12;
        public static readonly byte ReadFanMask = 0x20;
        public static readonly byte ReadFanSpeed = 0x21;
        public static readonly byte ReadFanPower = 0x22;
        public static readonly byte WriteFanPower = 0x23;
        public static readonly byte WriteFanSpeed = 0x24;
        public static readonly byte WriteFanCurve = 0x25;
        public static readonly byte WriteFanExternalTemp = 0x26;
        public static readonly byte WriteFanForceThreePinMode = 0x27;
        public static readonly byte WriteFanDetectionType = 0x28;
        public static readonly byte ReadFanDetectionType = 0x29;
        public static readonly byte ReadLedStripMask = 0x30;
        public static readonly byte WriteLedRgbValue = 0x31;
        public static readonly byte WriteLedColorValues = 0x32;
        public static readonly byte WriteLedTrigger = 0x33;
        public static readonly byte WriteLedClear = 0x34;
        public static readonly byte WriteLedGroupSet = 0x35;
        public static readonly byte WriteLedExternalTemp = 0x36;
        public static readonly byte WriteLedGroupsClear = 0x37;
        public static readonly byte WriteLedMode = 0x38;
        public static readonly byte WriteLedBrightness = 0x39;
        public static readonly byte WriteLedCount = 0x3a;
        public static readonly byte WriteLedPortType = 0x3b;
    }
}
