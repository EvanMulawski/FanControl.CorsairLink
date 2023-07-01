using System.Buffers.Binary;
using System.Text;

namespace CorsairLink.Devices;

public sealed class CoolitDevice : DeviceBase
{
    private static class DeviceModels
    {
        public static DeviceModel CreateUnknown(byte id = default, string? name = default) => new(id, name ?? "Unknown", default, default, default);

        public static readonly DeviceModel HydroH80i = new(0x3b, "Hydro H80i", 5, 1, pumpChannel: 4, liquidTemperatureChannel: 0);
        public static readonly DeviceModel HydroH100i = new(0x3c, "Hydro H100i", 5, 1, pumpChannel: 4, liquidTemperatureChannel: 0);
        public static readonly DeviceModel CommanderMini = new(0x3d, "Commander Mini", 6, 4, iterateTemperatureSensorsInReverse: true);
        public static readonly DeviceModel HydroH100iGT = new(0x40, "Hydro H100i GT", 3, 1, pumpChannel: 2, liquidTemperatureChannel: 0);
        public static readonly DeviceModel HydroH110iGT = new(0x41, "Hydro H110i", 3, 1, pumpChannel: 2, liquidTemperatureChannel: 0);
        public static readonly DeviceModel HydroH110i = new(0x42, "Hydro H110i", 3, 1, pumpChannel: 2, liquidTemperatureChannel: 0);

        public static readonly IReadOnlyDictionary<byte, DeviceModel> Supported = new List<DeviceModel>
        {
            HydroH80i,
            HydroH100i,
            CommanderMini,
            HydroH100iGT,
            HydroH110iGT,
            HydroH110i,
        }.ToDictionary(x => x.Id);
    }

    private static class Registers
    {
        public static readonly byte ReadDeviceId = 0x00;
        public static readonly byte ReadFirmwareVersion = 0x01;
        public static readonly byte WriteCurrentFan = 0x10;
        public static readonly byte WriteFanMode = 0x12;
        public static readonly byte WriteFanPower = 0x13;
        public static readonly byte ReadFanSpeed = 0x16;
        public static readonly byte WriteCurrentTemperatureSensor = 0x0c;
        public static readonly byte ReadTemperatureValue = 0x0e;
    }

    private static class Operations
    {
        public static readonly byte WriteByte = 0x06;
        public static readonly byte ReadByte = 0x07;
        public static readonly byte WriteWord = 0x08;
        public static readonly byte ReadWord = 0x09;
        public static readonly byte WriteBlock = 0x0a;
        public static readonly byte ReadBlock = 0x0b;
    }

    private static class RegisterPayloads
    {
        public static ReadOnlySpan<byte> FixedPercentFanMode => new byte[] { 0x02 };
    }

    private readonly IHidDeviceProxy _device;
    private readonly IDeviceGuardManager _guardManager;
    private readonly ChannelTrackingStore _requestedChannelPower = new();
    private readonly Dictionary<int, SpeedSensor> _speedSensors = new();
    private readonly Dictionary<int, TemperatureSensor> _temperatureSensors = new();

    private const int REQUEST_LENGTH = 64;
    private const int RESPONSE_LENGTH = 64;
    private const int DEFAULT_SPEED_CHANNEL_POWER = 50;
    private const byte PERCENT_MIN = 0x00;
    private const byte PERCENT_MAX = 0x64;

    private byte _requestId = 0x00;
    private DeviceModel _model;
    private string _name;
    private string? _firmwareVersion;
    private readonly string _serialNumber;

    public CoolitDevice(IHidDeviceProxy device, IDeviceGuardManager guardManager, ILogger logger)
        : base(logger)
    {
        _device = device;
        _guardManager = guardManager;

        var deviceInfo = device.GetDeviceInfo();
        _serialNumber = deviceInfo.SerialNumber;
        _name = deviceInfo.ProductName;
        _model = DeviceModels.CreateUnknown();

        UniqueId = deviceInfo.DevicePath;
    }

    private string GetName() => $"{_name} ({_serialNumber})";

    public override string UniqueId { get; }

    public override string Name => GetName();

    public override IReadOnlyCollection<SpeedSensor> SpeedSensors => _speedSensors.Values;

    public override IReadOnlyCollection<TemperatureSensor> TemperatureSensors => _temperatureSensors.Values;

    public override bool Connect()
    {
        Disconnect();

        var (opened, exception) = _device.Open();
        if (opened)
        {
            var deviceInfo = QueryDevice();
            _model = deviceInfo.Model;

            if (!deviceInfo.IsSupported)
            {
                LogError($"Device model {_model.Id:X2} is not supported");
                return false;
            }

            _name = _model.Name;
            _firmwareVersion = deviceInfo.FirmwareVersion;

            Initialize();
            return true;
        }

        if (exception is not null)
        {
            LogError(exception);
        }

        return false;
    }

    private DeviceInfo QueryDevice()
    {
        var modelId = GetDeviceModelId();
        var isSupported = DeviceModels.Supported.TryGetValue(modelId, out var model);
        var firmwareVersion = isSupported ? GetDeviceFirmwareVersion() : default;

        return new DeviceInfo(model ?? DeviceModels.CreateUnknown(modelId), firmwareVersion, isSupported);
    }

    private byte GetDeviceModelId()
    {
        var request = CreateRequest(GetNextRequestId(), Operations.ReadWord, Registers.ReadDeviceId);
        var response = WriteAndRead(request);
        return ParseDeviceModelId(response);
    }

    private string GetDeviceFirmwareVersion()
    {
        var request = CreateRequest(GetNextRequestId(), Operations.ReadWord, Registers.ReadFirmwareVersion);
        var response = WriteAndRead(request);
        return ParseDeviceFirmwareVersion(response);
    }

    public override void Disconnect()
    {
        _device.Close();
    }

    private void Initialize()
    {
        InitializeRequestedChannelPower();
        RefreshImpl(initialize: true);
    }

    public override string GetFirmwareVersion() => _firmwareVersion ?? "?";

    public override void Refresh() => RefreshImpl();

    private void RefreshImpl(bool initialize = false)
    {
        WriteRequestedSpeeds(setMode: initialize);
        RefreshTemperatures();
        RefreshSpeeds();

        if (CanLogDebug)
        {
            LogDebug(GetStateStringRepresentation());
        }
    }

    public override void SetChannelPower(int channel, int percent)
    {
        _requestedChannelPower[channel] = (byte)Utils.Clamp(percent, PERCENT_MIN, PERCENT_MAX);
    }

    private void InitializeRequestedChannelPower()
    {
        _requestedChannelPower.Clear();

        for (int i = 0; i < _model.SpeedChannelCount; i++)
        {
            _requestedChannelPower[i] = DEFAULT_SPEED_CHANNEL_POWER;
        }
    }

    private void RefreshTemperatures()
    {
        var sensors = GetTemperatureSensors();

        foreach (var sensor in sensors)
        {
            if (!_temperatureSensors.TryGetValue(sensor.Channel, out var existingSensor))
            {
                _temperatureSensors[sensor.Channel] = sensor;
                continue;
            }

            existingSensor.TemperatureCelsius = sensor.TemperatureCelsius;
        }
    }

    private void RefreshSpeeds()
    {
        var sensors = GetSpeedSensors();

        foreach (var sensor in sensors)
        {
            if (!_speedSensors.TryGetValue(sensor.Channel, out var existingSensor))
            {
                _speedSensors[sensor.Channel] = sensor;
                continue;
            }

            existingSensor.Rpm = sensor.Rpm;
        }
    }

    private int GetFanRpm(int channelId)
    {
        byte[] response;

        using (_guardManager.AwaitExclusiveAccess())
        {
            SelectFanChannel(channelId);

            var request = CreateRequest(GetNextRequestId(), Operations.ReadWord, Registers.ReadFanSpeed);
            response = WriteAndRead(request, guard: false);
        }

        return ParseFanRpmValue(response);
    }

    private void SelectFanChannel(int channelId)
    {
        var payload = new byte[]
        {
            (byte)channelId
        };

        var request = CreateRequest(GetNextRequestId(), Operations.WriteByte, Registers.WriteCurrentFan, payload);
        _ = WriteAndRead(request, guard: false);
    }

    private void SetFanPower(int channelId, byte percent, bool setMode)
    {
        var payload = new byte[]
        {
            CreateFanPowerValue(percent)
        };

        byte[] request;

        using (_guardManager.AwaitExclusiveAccess())
        {
            SelectFanChannel(channelId);

            if (setMode)
            {
                request = CreateRequest(GetNextRequestId(), Operations.WriteByte, Registers.WriteFanMode, RegisterPayloads.FixedPercentFanMode);
                _ = WriteAndRead(request, guard: false);
            }

            request = CreateRequest(GetNextRequestId(), Operations.WriteByte, Registers.WriteFanPower, payload);
            _ = WriteAndRead(request, guard: false);
        }
    }

    private void WriteRequestedSpeeds(bool setMode)
    {
        if (!_requestedChannelPower.ApplyChanges())
        {
            return;
        }

        foreach (var c in _requestedChannelPower.Channels)
        {
            SetFanPower(c, _requestedChannelPower[c], setMode);
        }
    }

    private void SelectTemperatureSensorChannel(int channelId)
    {
        var payload = new byte[]
        {
            (byte)channelId
        };

        var request = CreateRequest(GetNextRequestId(), Operations.WriteByte, Registers.WriteCurrentTemperatureSensor, payload);
        _ = WriteAndRead(request, guard: false);
    }

    private float? GetTemperatureSensorValue(int channelId)
    {
        byte[] response;

        using (_guardManager.AwaitExclusiveAccess())
        {
            SelectTemperatureSensorChannel(channelId);

            var request = CreateRequest(GetNextRequestId(), Operations.ReadWord, Registers.ReadTemperatureValue);
            response = WriteAndRead(request, guard: false);
        }

        return ParseTemperatureSensorValue(response);
    }

    private IReadOnlyCollection<SpeedSensor> GetSpeedSensors()
    {
        var sensors = new List<SpeedSensor>();

        for (int ch = 0; ch < _model.SpeedChannelCount; ch++)
        {
            var rpm = GetFanRpm(ch);

            if (_model.HasPump && ch == _model.PumpChannel)
            {
                sensors.Add(new SpeedSensor($"Pump", ch, rpm, supportsControl: true));
            }
            else
            {
                sensors.Add(new SpeedSensor($"Fan #{ch + 1}", ch, rpm, supportsControl: true));
            }
        }

        return sensors;
    }

    private IReadOnlyCollection<TemperatureSensor> GetTemperatureSensors()
    {
        var sensors = new List<TemperatureSensor>();

        for (int ch = 0; ch < _model.TemperatureSensorCount; ch++)
        {
            var temp = GetTemperatureSensorValue(ch);

            if (_model.HasPump && ch == _model.LiquidTemperatureChannel)
            {
                sensors.Add(new TemperatureSensor("Liquid Temp", ch, temp));
            }
            else
            {
                var chDisplay = (_model.IterateTemperatureSensorsInReverse ? _model.TemperatureSensorCount - 1 - ch : ch) + 1;
                sensors.Add(new TemperatureSensor($"Temp #{chDisplay}", ch, temp));
            }
        }

        return sensors;
    }

    private byte[] WriteAndRead(byte[] buffer, bool guard = true)
    {
        var response = CreateResponse();

        if (guard)
        {
            using (_guardManager.AwaitExclusiveAccess())
            {
                Write(buffer);
                Read(response);
            }
        }
        else
        {
            Write(buffer);
            Read(response);
        }

        return response;
    }

    private void Write(byte[] buffer)
    {
        _device.Write(buffer);
    }

    private void Read(byte[] buffer)
    {
        _device.Read(buffer);
    }

    private byte GetNextRequestId() => _requestId++;

    private static byte[] CreateRequest(byte requestId, byte operation, byte register, ReadOnlySpan<byte> data = default)
    {
        // [0] report id (0)
        // [1] length
        // [2] command/request id (incremented byte) - doesn't look like this matters whatsoever
        // [3] command opcode
        // [4] register address
        // [5,] register data

        var writeBuf = new byte[REQUEST_LENGTH];
        writeBuf[1] = (byte)(3 + data.Length);
        writeBuf[2] = requestId;
        writeBuf[3] = operation;
        writeBuf[4] = register;

        if (data.Length > 0)
        {
            var writeBufData = writeBuf.AsSpan(5);
            data.CopyTo(writeBufData);
        }

        return writeBuf;
    }

    private static byte[] CreateResponse()
    {
        return new byte[RESPONSE_LENGTH];
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

    internal static string ParseDeviceFirmwareVersion(ReadOnlySpan<byte> data)
    {
        return string.Format("{0}.{1}.{2}", (data[4] & 0xf0) >> 4, data[4] & 0x0f, data[3]);
    }

    internal static byte ParseDeviceModelId(ReadOnlySpan<byte> data)
    {
        if (data[4] == 0)
        {
            return data[3];
        }

        return default;
    }

    internal static int ParseFanRpmValue(ReadOnlySpan<byte> data)
    {
        return BinaryPrimitives.ReadInt16LittleEndian(data.Slice(3, 2));
    }

    internal static byte CreateFanPowerValue(byte percent)
    {
        var percentValid = (byte)Utils.Clamp(percent, PERCENT_MIN, PERCENT_MAX);
        return (byte)(Math.Min(percentValid / 100.0, 1.0) * byte.MaxValue);
    }

    internal static short? ParseTemperatureSensorValue(ReadOnlySpan<byte> data)
    {
        var result = (short)(BinaryPrimitives.ReadInt16LittleEndian(data.Slice(3, 2)) / 256);
        return result >= 0 ? result : null;
    }

    private sealed class DeviceModel
    {
        public DeviceModel(
            byte id,
            string name,
            int speedChannelCount,
            int temperatureSensorCount,
            int? pumpChannel = default,
            int? liquidTemperatureChannel = default,
            bool iterateTemperatureSensorsInReverse = false)
        {
            Id = id;
            Name = name;
            SpeedChannelCount = speedChannelCount;
            TemperatureSensorCount = temperatureSensorCount;
            PumpChannel = pumpChannel;
            LiquidTemperatureChannel = liquidTemperatureChannel;
            IterateTemperatureSensorsInReverse = iterateTemperatureSensorsInReverse;
        }

        public byte Id { get; }
        public string Name { get; }
        public int SpeedChannelCount { get; }
        public int TemperatureSensorCount { get; }
        public int? PumpChannel { get; }
        public int? LiquidTemperatureChannel { get; }
        public bool IterateTemperatureSensorsInReverse { get; }

        public bool HasPump => PumpChannel is not null;
    }

    private sealed class DeviceInfo
    {
        public DeviceInfo(
            DeviceModel model,
            string? firmwareVersion,
            bool isSupported)
        {
            Model = model;
            FirmwareVersion = firmwareVersion;
            IsSupported = isSupported;
        }

        public DeviceModel Model { get; }
        public string? FirmwareVersion { get; }
        public bool IsSupported { get; }
    }
}
