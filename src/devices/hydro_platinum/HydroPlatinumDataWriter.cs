namespace CorsairLink.Devices.HydroPlatinum;

public class HydroPlatinumDataWriter
{
    public const int PACKET_SIZE = 64;
    public const int HID_PACKET_SIZE = PACKET_SIZE + 1; // HID Report ID
    public const int PAYLOAD_START_IDX = 1;
    public const int PAYLOAD_DATA_START_IDX = PAYLOAD_START_IDX + 1;
    public const int PAYLOAD_LENGTH = PACKET_SIZE - PAYLOAD_START_IDX;

    public virtual byte[] CreateHidPacketBuffer()
    {
        return new byte[HID_PACKET_SIZE];
    }

    public virtual byte[] CreateHidPacket(ReadOnlySpan<byte> payload)
    {
        var buffer = CreateHidPacketBuffer();
        payload.CopyTo(buffer.AsSpan(1));
        return buffer;
    }

    public virtual byte[] CreateCommandPacket(byte command, byte sequenceNumber, ReadOnlySpan<byte> data, byte fill = 0xff)
    {
        // [0] = 0x3f (63, XMCPayloadLength)
        // [1] = sequenceNumber | command
        // [2,] = data
        // [-1] = CRC byte

        var writeBuf = new byte[PACKET_SIZE];
        writeBuf[0] = PAYLOAD_LENGTH;
        writeBuf[1] = (byte)(sequenceNumber | command);
        var writeBufData = writeBuf.AsSpan(PAYLOAD_DATA_START_IDX);
        writeBufData.Fill(fill);

        if (data.Length > 0)
        {
            data.CopyTo(writeBufData);
        }

        var writeCrcByte = CalculateChecksumByte(writeBuf);
        writeBuf[writeBuf.Length - 1] = writeCrcByte;

        return writeBuf;
    }

    public virtual void SetChecksumByte(byte[] packet)
    {
        packet[packet.Length - 1] = CalculateChecksumByte(packet);
    }

    public virtual byte CalculateChecksumByte(ReadOnlySpan<byte> packet)
    {
        var bytesForChecksum = packet.Slice(PAYLOAD_START_IDX, packet.Length - PAYLOAD_START_IDX - 1);
        return Crc8Ccitt.CalculateChecksumByte(bytesForChecksum);
    }

    public virtual ReadOnlySpan<byte> CreateIncomingStateCommandData()
    {
        byte[] data = [
            0xff,
            0x00,
        ];

        return data;
    }

    public virtual ReadOnlySpan<byte> CreateCoolingCommandData(byte fan0ChannelPower, byte? fan1ChannelPower = default, PumpMode? pumpMode = default)
    {
        var data = new byte[PAYLOAD_LENGTH - 1 - 12];
        data.AsSpan().Fill(0xff);

        // [0,5] = start of fan 0 data
        // [6,11] = start of fan 1 data
        // [12,17] = start of pump data
        // [18,] = fan curve data

        // fan data:
        // [0] = mode (fixed percent = 0x02, fixed percent w/ fallback aka zero-rpm = 0x03)
        // [1] = fixed speed lower byte
        // [2] = fixed speed upper byte
        // [3] = external temperature lower byte
        // [4] = external temperature upper byte
        // [5] = fixed percent

        // pump data:
        // [0] = mode (balanced = 0x01)
        // [1] = 0xff
        // [2] = 0xff
        // [3] = external temperature lower byte
        // [4] = external temperature upper byte
        // [5] = 0xff

        data[0] = 0x03;
        data[1] = 0x00;
        data[2] = 0x00;
        data[3] = 0x00;
        data[4] = 0x00;
        data[5] = fan0ChannelPower;

        if (fan1ChannelPower.HasValue)
        {
            data[6] = 0x03;
            data[7] = 0x00;
            data[8] = 0x00;
            data[9] = 0x00;
            data[10] = 0x00;
            data[11] = fan1ChannelPower.Value;
        }

        if (pumpMode.HasValue)
        {
            data[12] = (byte)pumpMode.Value;
            data[15] = 0x00;
            data[16] = 0x00;
        }

        return data;
    }

    public virtual ReadOnlySpan<byte> CreateCoolingCommand(ReadOnlySpan<byte> data)
    {
        // [0] = 0x14 (20, DefaultCountdownTimer)
        // [1] = store in EEPROM (yes=0x55, no=0x00)
        // [2] = 0xff
        // [3] = 0x05
        // [4,8] = fan safety profile (not using)

        var coolingPayload = new byte[PAYLOAD_LENGTH - 1 - 3];
        coolingPayload.AsSpan().Fill(0xff);
        coolingPayload[0] = 0x14;
        coolingPayload[1] = 0x00;
        coolingPayload[2] = 0xff;
        coolingPayload[3] = 0x05;

        if (data.Length > 0)
        {
            var dataSpan = coolingPayload.AsSpan(9);
            data.CopyTo(dataSpan);
        }

        return coolingPayload;
    }

    public virtual ReadOnlySpan<byte> CreateDirectLightingConfigurationPacket(byte sequenceNumber, bool enableDirectLighting, int brightnessPercent)
    {
        byte enableByte = enableDirectLighting ? (byte)0x01 : default;
        byte brightnessByte = Utils.ToFractionalByte(brightnessPercent);

        byte[] data = [
            enableByte,
            0x01,
            0xff,
            0xff,
            0xff,
            0xff,
            0xff,
            0xff,
            0xff,
            0xff,
            0xff,
            0xff,
            0xff,
            0xff,
            0xff,
            0x7f,
            0x7f,
            0x7f,
            0x7f,
            0x7f,
            0x00,
            0xff,
            0xff,
            0xff,
            0xff,
            0x00,
            0xff,
            0xff,
            0xff,
            0xff,
            0x00,
            0xff,
            0xff,
            0xff,
            0xff,
            0x00,
            0xff,
            0xff,
            0xff,
            0xff,
            0x00,
            0xff,
            0xff,
            0xff,
            0xff,
            0x00,
            0xff,
            0xff,
            0xff,
            0xff,
            brightnessByte,
            0xff,
            0xff,
            0xff,
            0xff,
            0xff,
            0xff,
            0xff,
            0xff,
            0xff,
            0xff,
            0xff,
        ];

        return CreateCommandPacket(Commands.DirectLightingConfiguration, sequenceNumber, data);
    }

    public virtual byte[] CreateDefaultLightingIndexData()
    {
        var data = new byte[80];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (byte)i;
        }
        return data;
    }

    public virtual byte[] CreateDefaultLightingColorData(byte r, byte g, byte b)
    {
        var data = new byte[80];
        for (int i = 0; i < data.Length - 3; i += 3)
        {
            data[i] = b;
            data[i + 1] = g;
            data[i + 2] = r;
        }
        return data;
    }

    public virtual ReadOnlySpan<byte> CreateRebootPacket()
    {
        var rebootPacket = new byte[PACKET_SIZE];
        byte[] rebootData = [0x52, 0x45, 0x42, 0x4F, 0x4F, 0x54];
        rebootData.CopyTo(rebootPacket, 1);
        return rebootPacket;
    }
}
