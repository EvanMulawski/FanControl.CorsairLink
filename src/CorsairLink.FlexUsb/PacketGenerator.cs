using System.Linq;

namespace CorsairLink.FlexUsb;

public static class PacketGenerator
{
    public static byte[] CreateWriteSMBusSettingsBuffer() => new byte[7]
    {
        (byte)ActionCode.WriteSMBusSettings,
        DefaultWriteSMBusSettings.Addr,
        DefaultWriteSMBusSettings.Bitrate,
        DefaultWriteSMBusSettings.WrIndex,
        DefaultWriteSMBusSettings.WrLen,
        DefaultWriteSMBusSettings.RdIndex,
        DefaultWriteSMBusSettings.RdLen,
    };

    public static byte[] CreateWriteSMBusCommandForReadBuffer(CommandCode command, int length)
    {
        return new byte[7]
        {
            (byte)ActionCode.WriteSMBusCommand,
            DefaultWriteSMBusCommandForRead.CommandNum,
            DefaultWriteSMBusCommandForRead.WrIndex,
            DefaultWriteSMBusCommandForRead.WrLen,
            DefaultWriteSMBusCommandForRead.RdIndex,
            (byte)length,
            (byte)command,
        };
    }

    public static byte[] CreateWriteSMBusCommandForWriteBuffer(CommandCode command, byte[] data)
    {
        return (new byte[5]
        {
            (byte)ActionCode.WriteSMBusCommand,
            DefaultWriteSMBusCommandForWrite.CommandNum,
            DefaultWriteSMBusCommandForWrite.WrIndex,
            (byte)(data.Length + 1),
            (byte)command,
        }).Concat(data).ToArray();
    }

    public static byte[] CreateReadVersionBuffer() => new byte[1]
    {
        (byte)ActionCode.ReadVersion,
    };

    public static byte[] CreateReadMemoryBuffer(int length) => new byte[3]
    {
        (byte)ActionCode.ReadMemory,
        DefaultReadMemory.Index,
        (byte)length,
    };

    public static byte[] CreateReadSMBusCommandBuffer() => new byte[1]
    {
        (byte)ActionCode.ReadSMBusCommand,
    };

    public static byte[] CreateWriteOutput12VOverCurrentLimitFaultBuffer() => new byte[2]
    {
        byte.MaxValue,
        byte.MaxValue,
    };

    private static class DefaultWriteSMBusSettings
    {
        public const int Addr = 2;
        public const int Bitrate = 100;
        public const int WrIndex = 0;
        public const int WrLen = 0;
        public const int RdIndex = 0;
        public const int RdLen = 0;
    }

    private static class DefaultWriteSMBusCommandForRead
    {
        public const int CommandNum = 3;
        public const int WrIndex = 6;
        public const int WrLen = 1;
        public const int RdIndex = 7;
    }

    private static class DefaultWriteSMBusCommandForWrite
    {
        public const int CommandNum = 1;
        public const int WrIndex = 4;
    }

    private static class DefaultReadMemory
    {
        public const int Index = 7;
    }
}
