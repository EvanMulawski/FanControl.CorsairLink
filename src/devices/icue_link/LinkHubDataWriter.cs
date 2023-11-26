using System.Buffers.Binary;

namespace CorsairLink.Devices.ICueLink;

public static class LinkHubDataWriter
{
    public static byte[] CreateSoftwareSpeedFixedPercentData(IReadOnlyDictionary<int, byte> channelSpeeds)
    {
        // data format:
        // [0] number of channels
        // [1,] channel data (only connected channels)

        // channel data:
        // [0] = channel id
        // [1] = 0x00
        // [2] = percent
        // [3] = 0x00

        var data = new byte[channelSpeeds.Count * 4 + 1];
        data[0] = (byte)channelSpeeds.Count;
        var i = 1;

        foreach (var channel in channelSpeeds.Keys)
        {
            data[i] = (byte)channel;
            data[i + 2] = channelSpeeds[channel];
            i += 4;
        }

        return data;
    }

    public static byte[] CreateWriteData(ReadOnlySpan<byte> dataType, ReadOnlySpan<byte> data)
    {
        const int HEADER_LENGTH = 4;

        // [0,1] = payload length
        // [2,3] = 0x00 0x00
        // [4,5] = data type
        // [6,]  = data

        var writeBuf = new byte[dataType.Length + data.Length + HEADER_LENGTH];
        BinaryPrimitives.WriteInt16LittleEndian(writeBuf.AsSpan(0, 2), (short)(data.Length + 2));
        dataType.CopyTo(writeBuf.AsSpan(HEADER_LENGTH, dataType.Length));
        data.CopyTo(writeBuf.AsSpan(HEADER_LENGTH + dataType.Length, data.Length));

        return writeBuf;
    }

    public static byte[] CreateCommandPacket(int bufferSize, ReadOnlySpan<byte> command, ReadOnlySpan<byte> data)
    {
        const int HEADER_LENGTH = 3;

        // [0] = 0x00
        // [1] = 0x00
        // [2] = 0x01
        // [3,a] = command
        // [a+1,] = data

        var writeBuf = new byte[bufferSize];
        writeBuf[2] = 0x01;

        var commandSpan = writeBuf.AsSpan(HEADER_LENGTH, command.Length);
        command.CopyTo(commandSpan);

        if (data.Length > 0)
        {
            var dataSpan = writeBuf.AsSpan(HEADER_LENGTH + commandSpan.Length, data.Length);
            data.CopyTo(dataSpan);
        }

        return writeBuf;
    }
}
