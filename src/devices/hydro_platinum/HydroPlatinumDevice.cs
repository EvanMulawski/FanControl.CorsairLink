using System.Buffers.Binary;
using System.Text;

namespace CorsairLink.Devices;

public sealed class HydroPlatinumDevice : DeviceBase
{
    internal static class Commands
    {
        public static readonly byte IncomingState = 0x00;
        public static readonly byte Cooling = 0x00;
        public static readonly byte CoolingThreeFanPacket = 0x03; // H150iXTOutgoingFanPacket
    }

    internal enum PumpMode : byte
    {
        Quiet = 0x00,
        Balanced = 0x01,
        Performance = 0x02,
    }

    private const int REQUEST_LENGTH = 65;
    private const int RESPONSE_LENGTH = 65;
    private const int DEVICE_WAIT_BEFORE_READ_DELAY_MS = 50;
    private const int DEVICE_PAYLOAD_START_IDX = 2;
    private const byte DEVICE_PAYLOAD_LENGTH = 63;
    private const byte DEVICE_PAYLOAD_LENGTH_EX_CRC = DEVICE_PAYLOAD_LENGTH - 1;
    private const int DEFAULT_SPEED_CHANNEL_POWER = 50;
    private const byte PERCENT_MIN = 0;
    private const byte PERCENT_MAX = 100;
    private const int PUMP_CHANNEL = -1;

    private readonly IHidDeviceProxy _device;
    private readonly IDeviceGuardManager _guardManager;
    private readonly uint _fanCount;
    private readonly ChannelTrackingStore _requestedChannelPower = new();
    private readonly Dictionary<int, SpeedSensor> _speedSensors = new();
    private readonly Dictionary<int, TemperatureSensor> _temperatureSensors = new();
    private readonly SequenceCounter _sequenceCounter = new();

    public HydroPlatinumDevice(IHidDeviceProxy device, IDeviceGuardManager guardManager, HydroPlatinumDeviceOptions options, ILogger logger)
        : base(logger)
    {
        _device = device;
        _guardManager = guardManager;

        var deviceInfo = device.GetDeviceInfo();
        Name = $"{deviceInfo.ProductName} ({deviceInfo.SerialNumber})";
        UniqueId = deviceInfo.DevicePath;

        _fanCount = options.FanChannelCount;
    }

    public override string UniqueId { get; }

    public override string Name { get; }

    public override IReadOnlyCollection<SpeedSensor> SpeedSensors => _speedSensors.Values;

    public override IReadOnlyCollection<TemperatureSensor> TemperatureSensors => _temperatureSensors.Values;

    public override bool Connect()
    {
        Disconnect();

        var (opened, exception) = _device.Open();
        if (opened)
        {
            Initialize();
            return true;
        }

        if (exception is not null)
        {
            LogError(exception);
        }

        return false;
    }

    public override void Disconnect()
    {
        _device.Close();
    }

    public override string GetFirmwareVersion()
    {
        State state;
        using (_guardManager.AwaitExclusiveAccess())
        {
            state = ReadState();
        }

        return $"{state.FirmwareVersionMajor}.{state.FirmwareVersionMinor}.{state.FirmwareVersionRevision}";
    }

    private void Initialize()
    {
        _requestedChannelPower.Clear();
        SetChannelPower(PUMP_CHANNEL, DEFAULT_SPEED_CHANNEL_POWER);
        _speedSensors[PUMP_CHANNEL] = new SpeedSensor("Pump", PUMP_CHANNEL, default, supportsControl: true);
        _temperatureSensors[PUMP_CHANNEL] = new TemperatureSensor("Liquid Temp", PUMP_CHANNEL, default);

        for (var i = 0; i < _fanCount; i++)
        {
            SetChannelPower(i, DEFAULT_SPEED_CHANNEL_POWER);
            _speedSensors[i] = new SpeedSensor($"Fan #{i + 1}", i, default, supportsControl: true);
        }
    }

    public override void Refresh()
    {
        // the device may not respond if attempting to read the state too quickly after other commands
        // a delay resolves this issue
        Utils.SyncWait(DEVICE_WAIT_BEFORE_READ_DELAY_MS);

        State state;
        using (_guardManager.AwaitExclusiveAccess())
        {
            state = ReadState();
            WriteCooling();
        }

        RefreshSensors(state);
    }

    public override void SetChannelPower(int channel, int percent)
    {
        _requestedChannelPower[channel] = Utils.ToFractionalByte(Utils.Clamp(percent, PERCENT_MIN, PERCENT_MAX));
    }

    private void RefreshSensors(State state)
    {
        _temperatureSensors[PUMP_CHANNEL].TemperatureCelsius = state.LiquidTempCelsius;
        _speedSensors[PUMP_CHANNEL].Rpm = state.PumpRpm;

        for (int i = 0; i < _fanCount; i++)
        {
            _speedSensors[i].Rpm = state.FanRpm[i];
        }
    }

    private void WriteCooling()
    {
        _requestedChannelPower.ApplyChanges();

        if (_fanCount == 3)
        {
            SendCoolingCommand(Commands.CoolingThreeFanPacket, CreateCoolingCommandData(_requestedChannelPower[2]));
        }

        SendCoolingCommand(Commands.Cooling, CreateCoolingCommandData(
            _requestedChannelPower[0],
            _requestedChannelPower[1],
            _requestedChannelPower[PUMP_CHANNEL]));
    }

    private State ReadState()
    {
        var stateResponse = SendCommand(Commands.IncomingState, CreateStateRequestData());
        var state = ParseState(stateResponse);
        _sequenceCounter.Set(state.SequenceNumber);
        return state;
    }

    internal State ParseState(ReadOnlySpan<byte> stateResponse)
    {
        var response = stateResponse;

        var fwMajor = response[2] >> 4;
        var fwMinor = response[2] & 15;
        var fwRevision = (int)response[3];
        var liquidTempRaw = (double)BinaryPrimitives.ReadInt16LittleEndian(response.Slice(7, 2));

        var state = new State
        {
            SequenceNumber = response[1],
            FirmwareVersionMajor = fwMajor,
            FirmwareVersionMinor = fwMinor,
            FirmwareVersionRevision = fwRevision,
            LiquidTempCelsius = (int)(liquidTempRaw / 25.6 + 0.5) / 10f,
            PumpMode = (PumpMode)response[24],
            PumpRpm = BinaryPrimitives.ReadInt16LittleEndian(response.Slice(29, 2)),
            FanRpm = new int[_fanCount]
        };

        state.FanRpm[0] = BinaryPrimitives.ReadInt16LittleEndian(response.Slice(15, 2));
        state.FanRpm[1] = BinaryPrimitives.ReadInt16LittleEndian(response.Slice(22, 2));

        if (_fanCount == 3)
        {
            state.FanRpm[2] = BinaryPrimitives.ReadInt16LittleEndian(response.Slice(43, 2));
        }

        if (CanLogDebug)
        {
            LogDebug($"STATE: {state}");
        }

        return state;
    }

    private ReadOnlySpan<byte> CreateStateRequestData()
    {
        var data = new byte[2];
        data[0] = 0xff;
        data[1] = 0x00;
        return data;
    }

    internal static ReadOnlySpan<byte> CreateCoolingCommandData(
        byte fan0ChannelPower,
        byte? fan1ChannelPower = default,
        byte? pumpChannelPower = default)
    {
        var data = new byte[DEVICE_PAYLOAD_LENGTH_EX_CRC - 12];
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

        if (pumpChannelPower.HasValue)
        {
            data[12] = (byte)GetPumpMode(pumpChannelPower.Value);
            data[15] = 0x00;
            data[16] = 0x00;
        }

        return data.ToArray();
    }

    internal static ReadOnlySpan<byte> CreateCoolingCommand(ReadOnlySpan<byte> data)
    {
        // ([3])    [0] = 0x14 (20, DefaultCountdownTimer)
        // ([4])    [1] = store in EEPROM (yes=0x55, no=0x00)
        // ([5])    [2] = 0xff
        // ([6])    [3] = 0x05
        // ([7,11]) [4,8] = fan safety profile (not using)

        var coolingPayload = new byte[DEVICE_PAYLOAD_LENGTH_EX_CRC - 3];
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

    private void SendCoolingCommand(byte command, ReadOnlySpan<byte> data)
    {
        SendWriteOnlyCommand(command, CreateCoolingCommand(data));
    }

    internal static ReadOnlySpan<byte> CreateCommand(byte command, byte sequenceNumber, ReadOnlySpan<byte> data)
    {
        // [0] = 0x00
        // [1] = 0x3f (63, XMCPayloadLength)
        // [2] = sequenceNumber | command
        // [3,] = data
        // [-1] = CRC byte

        var writeBuf = new byte[REQUEST_LENGTH];
        writeBuf.AsSpan(1).Fill(0xff);
        writeBuf[1] = DEVICE_PAYLOAD_LENGTH;

        // sequence number does not have to change for write to succeed
        writeBuf[2] = (byte)(sequenceNumber | command);

        if (data.Length > 0)
        {
            var dataSpan = writeBuf.AsSpan(3);
            data.CopyTo(dataSpan);
        }

        var writeCrcByte = GenerateChecksum(writeBuf.AsSpan(DEVICE_PAYLOAD_START_IDX, DEVICE_PAYLOAD_LENGTH_EX_CRC));
        writeBuf[writeBuf.Length - 1] = writeCrcByte;

        return writeBuf;
    }

    private void SendWriteOnlyCommand(byte command, ReadOnlySpan<byte> data)
    {
        var writeBuf = CreateCommand(command, _sequenceCounter.Next(), data).ToArray();

        try
        {
            Write(writeBuf);
        }
        catch (Exception ex)
        {
            throw CreateCommandException("Communication failure.", ex, writeBuf);
        }
    }

    private byte[] SendCommand(byte command, ReadOnlySpan<byte> data)
    {
        var readBuf = new byte[RESPONSE_LENGTH];
        var writeBuf = CreateCommand(command, _sequenceCounter.Next(), data).ToArray();
        byte readCrcByte, readBufCrcResult;

        try
        {
            WriteAndRead(writeBuf, readBuf);
            readCrcByte = readBuf[readBuf.Length - 1];
            readBufCrcResult = GenerateChecksum(readBuf.AsSpan(DEVICE_PAYLOAD_START_IDX, DEVICE_PAYLOAD_LENGTH_EX_CRC));
        }
        catch (Exception ex)
        {
            throw CreateCommandException("Communication failure.", ex, writeBuf, readBuf);
        }

        if (readCrcByte != readBufCrcResult)
        {
            throw CreateCommandException("Checksum failure.", default, writeBuf, readBuf);
        }

        return readBuf.AsSpan(1).ToArray();
    }

    private static CorsairLinkDeviceException CreateCommandException(string message, Exception? innerException, ReadOnlySpan<byte> writeBuffer, ReadOnlySpan<byte> readBuffer)
    {
        var exception = CreateCommandException(message, innerException, writeBuffer);
        exception.Data[nameof(readBuffer)] = readBuffer.ToHexString();
        return exception;
    }

    private static CorsairLinkDeviceException CreateCommandException(string message, Exception? innerException, ReadOnlySpan<byte> writeBuffer)
    {
        var exception = innerException is null
            ? new CorsairLinkDeviceException(message)
            : new CorsairLinkDeviceException(message, innerException);
        exception.Data[nameof(writeBuffer)] = writeBuffer.ToHexString();
        return exception;
    }

    private void WriteAndRead(byte[] writeBuffer, byte[] readBuffer)
    {
        if (CanLogDebug)
        {
            LogDebug($"WRITE: {writeBuffer.ToHexString()}");
        }

        _device.Write(writeBuffer);
        _device.Read(readBuffer);

        if (CanLogDebug)
        {
            LogDebug($"READ:  {readBuffer.ToHexString()}");
        }
    }

    private void Write(byte[] writeBuffer)
    {
        if (CanLogDebug)
        {
            LogDebug($"WRITE: {writeBuffer.ToHexString()}");
        }

        _device.Write(writeBuffer);
    }

    internal static byte GenerateChecksum(ReadOnlySpan<byte> data)
    {
        byte result = 0;
        for (int i = 0; i < data.Length; i++)
        {
            result = CRC8_CCITT_TABLE[result ^ data[i]];
        }
        return result;
    }

    private static readonly byte[] CRC8_CCITT_TABLE = new byte[256]
    {
        0, 7, 14, 9, 28, 27, 18, 21, 56, 63, 54, 49, 36, 35, 42, 45,
        112, 119, 126, 121, 108, 107, 98, 101, 72, 79, 70, 65, 84, 83, 90, 93,
        224, 231, 238, 233, 252, 251, 242, 245, 216, 223, 214, 209, 196, 195, 202, 205,
        144, 151, 158, 153, 140, 139, 130, 133, 168, 175, 166, 161, 180, 179, 186, 189,
        199, 192, 201, 206, 219, 220, 213, 210, 255, 248, 241, 246, 227, 228, 237, 234,
        183, 176, 185, 190, 171, 172, 165, 162, 143, 136, 129, 134, 147, 148, 157, 154,
        39, 32, 41, 46, 59, 60, 53, 50, 31, 24, 17, 22, 3, 4, 13, 10,
        87, 80, 89, 94, 75, 76, 69, 66, 111, 104, 97, 102, 115, 116, 125, 122,
        137, 142, 135, 128, 149, 146, 155, 156, 177, 182, 191, 184, 173, 170, 163, 164,
        249, 254, 247, 240, 229, 226, 235, 236, 193, 198, 207, 200, 221, 218, 211, 212,
        105, 110, 103, 96, 117, 114, 123, 124, 81, 86, 95, 88, 77, 74, 67, 68,
        25, 30, 23, 16, 5, 2, 11, 12, 33, 38, 47, 40, 61, 58, 51, 52,
        78, 73, 64, 71, 82, 85, 92, 91, 118, 113, 120, 127, 106, 109, 100, 99,
        62, 57, 48, 55, 34, 37, 44, 43, 6, 1, 8, 15, 26, 29, 20, 19,
        174, 169, 160, 167, 178, 181, 188, 187, 150, 145, 152, 159, 138, 141, 132, 131,
        222, 217, 208, 215, 194, 197, 204, 203, 230, 225, 232, 239, 250, 253, 244, 243,
    };

    internal sealed class State
    {
        public byte SequenceNumber { get; set; }
        public int FirmwareVersionMajor { get; set; }
        public int FirmwareVersionMinor { get; set; }
        public int FirmwareVersionRevision { get; set; }
        public int[] FanRpm { get; set; }
        public PumpMode PumpMode { get; set; }
        public int PumpRpm { get; set; }
        public float LiquidTempCelsius { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (var i = 0; i < FanRpm.Length; i++)
            {
                sb.AppendFormat("fan{0}Rpm={1}, ", i + 1, FanRpm[i]);
            }
            sb.AppendFormat("pumpMode={0}, ", PumpMode);
            sb.AppendFormat("pumpRpm={0}, ", PumpRpm);
            sb.AppendFormat("liquidTempCelsius={0}", LiquidTempCelsius);
            return sb.ToString();
        }
    }

    internal static PumpMode GetPumpMode(byte requestedPower)
    {
        var percent = Utils.FromFractionalByte(requestedPower);
        return percent switch
        {
            <= 33 => PumpMode.Quiet,
            <= 67 => PumpMode.Balanced,
            _ => PumpMode.Performance,
        };
    }

    private sealed class SequenceCounter
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
}
