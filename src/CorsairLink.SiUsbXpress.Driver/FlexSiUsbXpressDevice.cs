namespace CorsairLink.SiUsbXpress.Driver;

public sealed class FlexSiUsbXpressDevice : SiUsbXpressDevice
{
    private const uint MAX_BAUD_RATE = 115200U;
    private const uint DEFAULT_TIMEOUT = 200U;
    private const uint READ_BUFFER_SIZE = 16;

    public FlexSiUsbXpressDevice(SiUsbXpressDeviceInfo deviceInfo)
        : base(deviceInfo)
    {
    }

    protected override uint ReadBufferSize { get; } = READ_BUFFER_SIZE;

    public override void Open()
    {
        base.Open();

        _ = SiUsbXpressDriver.SI_SetTimeouts(DEFAULT_TIMEOUT, DEFAULT_TIMEOUT);
        FlushBuffers();
        _ = SiUsbXpressDriver.SI_SetBaudRate(DeviceHandle!.DangerousGetHandle(), MAX_BAUD_RATE);
    }

    protected override byte[] GetReadBuffer(byte[] originalBuffer)
    {
        var buffer = base.GetReadBuffer(originalBuffer);
        return !EncodingHelper.HasError(buffer)
            ? EncodingHelper.DecodeData(buffer)
            : throw new SiUsbXpressException("Failed to read - data error.");
    }

    protected override byte[] GetWriteBuffer(byte[] originalBuffer)
    {
        var buffer = base.GetWriteBuffer(originalBuffer);
        return EncodingHelper.EncodeData(buffer);
    }

    private AckStatus ReadAckStatus()
        => AckParser.Parse(Read());

    public override void WriteAndValidate(byte[] buffer)
    {
        ThrowIfDeviceNotReady();

        Write(buffer);
        AckStatus ackStatus = ReadAckStatus();
        if (ackStatus != AckStatus.Ok)
            throw new SiUsbXpressDeviceAckException(ackStatus);
    }

    public override void WriteWhileBusy(byte[] buffer)
    {
        ThrowIfDeviceNotReady();

        AckStatus ackStatus;
        do
        {
            Write(buffer);
            ackStatus = ReadAckStatus();
        }
        while (ackStatus == AckStatus.Busy);
        if (ackStatus != AckStatus.Ok)
            throw new SiUsbXpressDeviceAckException(ackStatus);
    }
}
