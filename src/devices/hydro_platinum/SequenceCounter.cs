namespace CorsairLink.Devices.HydroPlatinum;

public sealed class SequenceCounter
{
    private byte _sequenceId = 0x00;

    public byte Next()
    {
        do
        {
            _sequenceId += 0x08;
        }
        while (_sequenceId == 0x00);
        return _sequenceId;
    }

    public void Set(byte value)
    {
        _sequenceId = value;
    }
}
