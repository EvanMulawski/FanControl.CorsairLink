using CorsairLink.SiUsbXpress;

namespace CorsairLink.FlexUsb;

public class ModernPsuProtocol : FlexDongleUsbPsuProtocol
{
    public ModernPsuProtocol(ISiUsbXpressDevice device)
        : base(device)
    {
    }

    public override void Write(CommandCode command, byte[] buffer)
    {
        Device.WriteAndValidate(PacketGenerator.CreateWriteSMBusCommandForWriteBuffer(command, buffer));
        Utils.SyncWait(1);
        Device.WriteWhileBusy(PacketGenerator.CreateReadSMBusCommandBuffer());
        Utils.SyncWait(1); // needed
    }

    public override byte[] Read(CommandCode command, int length)
    {
        Device.WriteAndValidate(PacketGenerator.CreateWriteSMBusCommandForReadBuffer(command, length));
        Utils.SyncWait(1);
        Device.WriteWhileBusy(PacketGenerator.CreateReadSMBusCommandBuffer());
        Utils.SyncWait(1);
        var data = Device.WriteAndRead(PacketGenerator.CreateReadMemoryBuffer(length));
        Utils.SyncWait(1); // needed
        return data;
    }
}
