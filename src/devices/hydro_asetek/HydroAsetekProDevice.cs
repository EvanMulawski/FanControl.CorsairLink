using CorsairLink.Asetek;
using System.Buffers.Binary;
using System.Text;

namespace CorsairLink.Devices;

public sealed class HydroAsetekProDevice : DeviceBase
{
    internal static class Commands
    {
        public static readonly byte SetPumpPower = 0x30;
        public static readonly byte GetPumpSpeed = 0x31;
        public static readonly byte GetFanSpeed = 0x41;
        public static readonly byte SetFanPower = 0x42;
        public static readonly byte SetFanSafetyProfile = 0x4A;
        public static readonly byte GetFanSafetyProfile = 0x4B;
        public static readonly byte GetLiquidTemperature = 0xA9;
        public static readonly byte GetFirmwareVersion = 0xAA;
    }

    private const int DEFAULT_SPEED_CHANNEL_POWER = 50;
    private const byte PERCENT_MIN = 0;
    private const byte PERCENT_MAX = 100;
    private const int PUMP_CHANNEL = -1;
    private const byte FAN_CURVE_MAX_TEMP = 60;

    private readonly IAsetekDeviceProxy _device;
    private readonly AsetekDeviceInfo _deviceInfo;
    private readonly IDeviceGuardManager _guardManager;
    private readonly uint _fanCount;
    private readonly bool _canSetSafetyProfile;
    private readonly ChannelTrackingStore _requestedChannelPower = new();
    private readonly Dictionary<int, SpeedSensor> _speedSensors = new();
    private readonly Dictionary<int, TemperatureSensor> _temperatureSensors = new();

    public HydroAsetekProDevice(IAsetekDeviceProxy device, IDeviceGuardManager guardManager, HydroAsetekProDeviceOptions options, ILogger logger)
        : base(logger)
    {
        _device = device;
        _deviceInfo = device.GetDeviceInfo();
        _guardManager = guardManager;
        _fanCount = options.FanChannelCount;
        _canSetSafetyProfile = options.OverrideSafetyProfile ?? HydroAsetekProDeviceOptions.OverrideSafetyProfileDefault;

        UniqueId = _deviceInfo.DevicePath;
        Name = $"{_deviceInfo.ProductName} ({Utils.ToMD5HexString(UniqueId)})";
    }

    public override string UniqueId { get; }

    public override string Name { get; }

    public override IReadOnlyCollection<SpeedSensor> SpeedSensors => _speedSensors.Values;

    public override IReadOnlyCollection<TemperatureSensor> TemperatureSensors => _temperatureSensors.Values;

    public override bool Connect()
    {
        LogDebug("Connect");

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
        LogDebug("Disconnect");

        _device.Close();
    }

    public override string GetFirmwareVersion()
    {
        var response = WriteAndRead(CreateRequest(Commands.GetFirmwareVersion));
        response.ThrowIfError();
        var data = response.GetData();
        return $"{data[0]}.{data[1]}.{data[2]}.{data[3]}";
    }

    private void Initialize()
    {
        OverrideSafetyProfile();
        InitializeSpeedChannelStores();
        Refresh();
    }

    public override void Refresh()
    {
        WriteRequestedSpeeds();
        RefreshTemperatures();
        RefreshSpeeds();

        if (CanLogDebug)
        {
            LogDebug(GetStateStringRepresentation());
        }
    }

    private void OverrideSafetyProfile()
    {
        LogDebug("OverrideSafetyProfile");

        if (!_canSetSafetyProfile)
        {
            LogWarning($"Skipping safety profile override. Device may not function as expected.");
            return;
        }

        var newSafetyProfileRequest = new byte[21]
        {
            FAN_CURVE_MAX_TEMP, // [0] tMax
            FAN_CURVE_MAX_TEMP - 1, // [1] tRampStart
            FAN_CURVE_MAX_TEMP - 2, // [2] tActivate
            FAN_CURVE_MAX_TEMP - 3, // [3] tDeactivate
            PERCENT_MAX, // [4] first speed
            FAN_CURVE_MAX_TEMP, // [5] temp 1
            PERCENT_MAX, // [6] speed 1
            FAN_CURVE_MAX_TEMP, // [7] temp 2
            PERCENT_MAX, // [8] speed 2
            FAN_CURVE_MAX_TEMP, // [9] temp 3
            PERCENT_MAX, // [10] speed 3
            FAN_CURVE_MAX_TEMP, // [11] temp 4
            PERCENT_MAX, // [12] speed 4
            FAN_CURVE_MAX_TEMP, // [13] temp 5
            PERCENT_MAX, // [14] speed 5
            FAN_CURVE_MAX_TEMP, // [15] temp 6
            PERCENT_MAX, // [16] speed 6
            FAN_CURVE_MAX_TEMP, // [17] temp 7
            PERCENT_MAX, // [18] speed 7
            0,   // [19] crc
            0,   // [20] crc
        };
        var newSafetyProfile = newSafetyProfileRequest.AsSpan(0, 19);

        WriteChecksum(newSafetyProfileRequest.AsSpan(0, 19), newSafetyProfileRequest.AsSpan(19));
        var writeResponse = WriteAndRead(CreateRequest(Commands.SetFanSafetyProfile, newSafetyProfileRequest));
        if (writeResponse.IsError)
        {
            writeResponse.Throw("Failed to set safety profile.");
            return;
        }
        var readResponse = WriteAndRead(CreateRequest(Commands.GetFanSafetyProfile));
        readResponse.ThrowIfError();
        var readSafetyProfile = readResponse.GetData();

        if (!readSafetyProfile.SequenceEqual(newSafetyProfile))
        {
            LogWarning("Safety profile readback mismatch. Device may not function as expected.");
            return;
        }

        LogInfo("Successfully set safety profile.");
    }

    public override void SetChannelPower(int channel, int percent)
    {
        LogDebug($"SetChannelPower {channel} {percent}%");
        _requestedChannelPower[channel] = (byte)Utils.Clamp(percent, PERCENT_MIN, PERCENT_MAX);
    }

    private void InitializeSpeedChannelStores()
    {
        LogDebug("InitializeSpeedChannelStores");

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

    private void RefreshTemperatures()
    {
        _temperatureSensors[PUMP_CHANNEL].TemperatureCelsius = GetLiquidTemperature();
    }

    private void RefreshSpeeds()
    {
        _speedSensors[PUMP_CHANNEL].Rpm = GetPumpRpm();

        for (var i = 0; i < _fanCount; i++)
        {
            _speedSensors[i].Rpm = GetFanRpm(i);
        }
    }

    private int GetFanRpm(int channel)
    {
        LogDebug($"GetFanRpm {channel}");

        var requestData = new byte[1] { (byte)channel };
        var response = WriteAndRead(CreateRequest(Commands.GetFanSpeed, requestData));
        response.ThrowIfError();
        return BinaryPrimitives.ReadUInt16BigEndian(response.GetData().Slice(1));
    }

    private int GetPumpRpm()
    {
        LogDebug("GetPumpRpm");

        var response = WriteAndRead(CreateRequest(Commands.GetPumpSpeed));
        response.ThrowIfError();
        return BinaryPrimitives.ReadUInt16BigEndian(response.GetData());
    }

    private float GetLiquidTemperature()
    {
        LogDebug("GetLiquidTemperature");

        var response = WriteAndRead(CreateRequest(Commands.GetLiquidTemperature));
        response.ThrowIfError();

        var data = response.GetData();
        var wholePart = (float)(sbyte)data[0];
        var fracData = data[1];

        if (fracData > 9)
        {
            response.Throw($"{nameof(GetLiquidTemperature)} encountered a data error: fractional data overflow");
        }

        var fracPart = fracData * ((double)wholePart < 0.0 ? -0.1f : 0.1f);
        return wholePart + fracPart;
    }

    private void SetFanPower(int channel, byte percent)
    {
        LogDebug($"SetFanPower {channel} {percent}%");

        var requestData = new byte[2] { (byte)channel, percent };
        var response = WriteAndRead(CreateRequest(Commands.SetFanPower, requestData));
        response.ThrowIfError();
    }

    private void SetPumpPower(byte percent)
    {
        LogDebug($"SetPumpPower {percent}%");

        var requestData = new byte[1] { percent };
        var response = WriteAndRead(CreateRequest(Commands.SetPumpPower, requestData));
        response.ThrowIfError();
    }

    private void WriteRequestedSpeeds()
    {
        LogDebug("WriteRequestedSpeeds");

        if (!_requestedChannelPower.ApplyChanges())
        {
            return;
        }

        SetPumpPower(_requestedChannelPower[PUMP_CHANNEL]);

        for (var i = 0; i < _fanCount; i++)
        {
            SetFanPower(i, _requestedChannelPower[i]);
        }
    }

    private string GetStateStringRepresentation()
    {
        var sb = new StringBuilder().AppendLine("STATE");

        foreach (var channel in _requestedChannelPower.Channels)
        {
            sb.AppendLine($"Requested power for channel {channel}: {_requestedChannelPower[channel]} %");
        }

        foreach (var sensor in SpeedSensors)
        {
            sb.AppendLine(sensor.ToString());
        }

        foreach (var sensor in TemperatureSensors)
        {
            sb.AppendLine(sensor.ToString());
        }

        return sb.ToString();
    }

    private DeviceResponse WriteAndRead(byte[] buffer)
    {
        DeviceResponse response;

        if (CanLogDebug)
        {
            LogDebug($"WRITE: {buffer.ToHexString()}");
        }

        using (_guardManager.AwaitExclusiveAccess())
        {
            var data = _device.WriteAndRead(buffer);

            if (CanLogDebug)
            {
                LogDebug($"READ:  {data.ToHexString()}");
            }

            response = new DeviceResponse(buffer, data);
        }

        return response;
    }

    private byte[] CreateRequest(byte command, ReadOnlySpan<byte> data = default)
    {
        var buffer = new byte[data.Length + 1];
        buffer[0] = command;
        data.CopyTo(buffer.AsSpan(1));
        return buffer;
    }

    internal sealed class DeviceResponse
    {
        public DeviceResponse(byte[] request, byte[] response)
        {
            Request = request;
            Response = response;
            IsError = !IsValid();
        }

        public byte[] Request { get; }
        private byte[] Response { get; }
        public bool IsError { get; }

        public ReadOnlySpan<byte> GetData() => Response.AsSpan(3);

        public void ThrowIfError()
        {
            if (IsError)
            {
                throw CreateException("Response was invalid.", Request, Response);
            }
        }

        public void Throw(string message)
        {
            throw CreateException(message, Request, Response);
        }

        private static CorsairLinkDeviceException CreateException(string message, ReadOnlySpan<byte> request, ReadOnlySpan<byte> response)
        {
            var exception = new CorsairLinkDeviceException(message);
            exception.Data[nameof(request)] = request.ToHexString();
            exception.Data[nameof(response)] = response.ToHexString();
            return exception;
        }

        public bool IsValid()
        {
            return Response[0] == Request[0];
        }
    }

    internal static ushort GenerateChecksum(ReadOnlySpan<byte> data)
    {
        ushort result = 0;
        for (int i = 0; i < data.Length; i++)
        {
            result = (ushort)((result >> 8) ^ CRC16_CCITT_TABLE[(result ^ data[i]) & 0xFF]);
        }
        return result;
    }

    internal static void WriteChecksum(ReadOnlySpan<byte> data, Span<byte> destination)
    {
        var checksum = GenerateChecksum(data);
        // device requires CRC in big endian
        BinaryPrimitives.WriteUInt16BigEndian(destination, checksum);
    }

    // polynomial: 33,800
    private static readonly ushort[] CRC16_CCITT_TABLE = new ushort[256]
    {
        0, 4489, 8978, 12955, 17956, 22445, 25910, 29887,
        35912, 40385, 44890, 48851, 51820, 56293, 59774, 63735,
        4225, 264, 13203, 8730, 22181, 18220, 30135, 25662,
        40137, 36160, 49115, 44626, 56045, 52068, 63999, 59510,
        8450, 12427, 528, 5017, 26406, 30383, 17460, 21949,
        44362, 48323, 36440, 40913, 60270, 64231, 51324, 55797,
        12675, 8202, 4753, 792, 30631, 26158, 21685, 17724,
        48587, 44098, 40665, 36688, 64495, 60006, 55549, 51572,
        16900, 21389, 24854, 28831, 1056, 5545, 10034, 14011,
        52812, 57285, 60766, 64727, 34920, 39393, 43898, 47859,
        21125, 17164, 29079, 24606, 5281, 1320, 14259, 9786,
        57037, 53060, 64991, 60502, 39145, 35168, 48123, 43634,
        25350, 29327, 16404, 20893, 9506, 13483, 1584, 6073,
        61262, 65223, 52316, 56789, 43370, 47331, 35448, 39921,
        29575, 25102, 20629, 16668, 13731, 9258, 5809, 1848,
        65487, 60998, 56541, 52564, 47595, 43106, 39673, 35696,
        33800, 38273, 42778, 46739, 49708, 54181, 57662, 61623,
        2112, 6601, 11090, 15067, 20068, 24557, 28022, 31999,
        38025, 34048, 47003, 42514, 53933, 49956, 61887, 57398,
        6337, 2376, 15315, 10842, 24293, 20332, 32247, 27774,
        42250, 46211, 34328, 38801, 58158, 62119, 49212, 53685,
        10562, 14539, 2640, 7129, 28518, 32495, 19572, 24061,
        46475, 41986, 38553, 34576, 62383, 57894, 53437, 49460,
        14787, 10314, 6865, 2904, 32743, 28270, 23797, 19836,
        50700, 55173, 58654, 62615, 32808, 37281, 41786, 45747,
        19012, 23501, 26966, 30943, 3168, 7657, 12146, 16123,
        54925, 50948, 62879, 58390, 37033, 33056, 46011, 41522,
        23237, 19276, 31191, 26718, 7393, 3432, 16371, 11898,
        59150, 63111, 50204, 54677, 41258, 45219, 33336, 37809,
        27462, 31439, 18516, 23005, 11618, 15595, 3696, 8185,
        63375, 58886, 54429, 50452, 45483, 40994, 37561, 33584,
        31687, 27214, 22741, 18780, 15843, 11370, 7921, 3960,
    };
}