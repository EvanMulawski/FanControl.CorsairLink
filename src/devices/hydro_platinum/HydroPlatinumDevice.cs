using CorsairLink.Devices.HydroPlatinum;

namespace CorsairLink.Devices;

public class HydroPlatinumDevice : DeviceBase
{
    protected const int DEVICE_INITIAL_WRITE_DELAY_MS = 60;
    protected const int DEFAULT_SPEED_CHANNEL_POWER = 50;
    protected const byte PERCENT_MIN = 0;
    protected const byte PERCENT_MAX = 100;
    protected const int MAX_READ_FAIL_BEFORE_REBOOT = 3;
    protected const int DEVICE_POST_REBOOT_WAIT_MS = 3000;
    private const int PUMP_CHANNEL = -1;

    protected readonly IHidDeviceProxy _device;
    protected readonly IDeviceGuardManager _guardManager;
    protected readonly int _fanCount;
    protected readonly bool _sendOnlyFirstLightingIndexPacket;
    protected readonly ChannelTrackingStore _requestedChannelPower = new();
    protected readonly Dictionary<int, SpeedSensor> _speedSensors = new();
    protected readonly Dictionary<int, TemperatureSensor> _temperatureSensors = new();
    protected readonly SequenceCounter _sequenceCounter = new();
    protected readonly RebootManager _rebootManager = new(MAX_READ_FAIL_BEFORE_REBOOT);
    protected readonly object _refreshLock = new();
    protected readonly DeviceMetrics _deviceMetrics;
    private readonly HydroPlatinumDataReader _dataReader;
    private readonly HydroPlatinumDataWriter _dataWriter;

    protected bool _rebootRequested;
    protected bool _firstWrite = true;
    protected bool _resetEnableDirectLightingOnNextRefresh;

    public HydroPlatinumDevice(IHidDeviceProxy device, IDeviceGuardManager guardManager, HydroPlatinumDeviceOptions options, ILogger logger)
        : base(logger)
    {
        _device = device;
        _guardManager = guardManager;
        _deviceMetrics = new DeviceMetrics(DEVICE_INITIAL_WRITE_DELAY_MS);

        var deviceInfo = device.GetDeviceInfo();
        Name = $"{deviceInfo.ProductName} ({deviceInfo.SerialNumber})";
        UniqueId = deviceInfo.DevicePath;

        _fanCount = options.FanChannelCount;
        _sendOnlyFirstLightingIndexPacket = deviceInfo.ProductId is 0x0c22 or 0x0c2f;
        _dataReader = new HydroPlatinumDataReader(_fanCount);
        _dataWriter = new HydroPlatinumDataWriter();
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
            _rebootManager.RebootRequired += RebootManager_RebootRequired;
            _rebootManager.RebootSuccessful += RebootManager_RebootSuccessful;
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
        _rebootManager.RebootRequired -= RebootManager_RebootRequired;
        _rebootManager.RebootSuccessful -= RebootManager_RebootSuccessful;
        _device.Close();
    }

    public override string GetFirmwareVersion()
    {
        HydroPlatinumDeviceState state;
        using (_guardManager.AwaitExclusiveAccess())
        {
            state = ReadState();
        }

        return $"{state.FirmwareVersionMajor}.{state.FirmwareVersionMinor}.{state.FirmwareVersionRevision}";
    }

    protected virtual void Initialize()
    {
        using (_guardManager.AwaitExclusiveAccess())
        {
            ResetEnableDirectLighting();
        }

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
        var lockTaken = false;

        try
        {
            Monitor.TryEnter(_refreshLock, 0, ref lockTaken);
            if (lockTaken)
            {
                RefreshImpl();
            }
            else
            {
                LogWarning($"Refresh skipped (refresh lock)");
            }
        }
        finally
        {
            if (lockTaken)
            {
                Monitor.Exit(_refreshLock);
            }
        }
    }

    protected virtual void RefreshImpl()
    {
        HydroPlatinumDeviceState state;
        using (_guardManager.AwaitExclusiveAccess())
        {
            TryResetEnableDirectLighting();

            state = ReadState();
            WriteCooling();
        }

        RefreshSensors(state);
    }

    protected void TryResetEnableDirectLighting()
    {
        if (_resetEnableDirectLightingOnNextRefresh)
        {
            ResetEnableDirectLighting();
            _resetEnableDirectLightingOnNextRefresh = false;
        }
    }

    public override void SetChannelPower(int channel, int percent)
    {
        _requestedChannelPower[channel] = Utils.ToFractionalByte(Utils.Clamp(percent, PERCENT_MIN, PERCENT_MAX));
    }

    private void RefreshSensors(HydroPlatinumDeviceState state)
    {
        _temperatureSensors[PUMP_CHANNEL].TemperatureCelsius = state.LiquidTempCelsius;
        _speedSensors[PUMP_CHANNEL].Rpm = state.PumpRpm;

        for (int i = 0; i < _fanCount; i++)
        {
            _speedSensors[i].Rpm = state.FanRpm[i];
        }
    }

    protected void WriteCooling()
    {
        _requestedChannelPower.ApplyChanges();

        if (_fanCount == 3)
        {
            SendCoolingCommand(Commands.CoolingThreeFanPacket, _dataWriter.CreateCoolingCommandData(_requestedChannelPower[2]));
        }

        SendCoolingCommand(Commands.Cooling, _dataWriter.CreateCoolingCommandData(
            _requestedChannelPower[0],
            _fanCount >= 2 ? _requestedChannelPower[1] : null,
            GetPumpMode(_requestedChannelPower[PUMP_CHANNEL])));
    }

    private HydroPlatinumDeviceState ReadState()
    {
        var data = _dataWriter.CreateIncomingStateCommandData();
        var stateResponse = SendCommand(Commands.IncomingState, data);
        var state = _dataReader.GetState(stateResponse);

        if (CanLogDebug)
        {
            LogDebug($"STATE: {state}");
        }

        _sequenceCounter.Set(state.SequenceNumber);
        return state;
    }

    private void SendCoolingCommand(byte command, ReadOnlySpan<byte> data)
    {
        SendWriteOnlyCommand(command, _dataWriter.CreateCoolingCommand(data));
    }

    private void SendWriteOnlyCommand(byte command, ReadOnlySpan<byte> data)
    {
        var writeBufData = _dataWriter.CreateCommandPacket(command, _sequenceCounter.Next(), data);
        var writeBuf = _dataWriter.CreateHidPacket(writeBufData);

        try
        {
            Write(writeBuf);
        }
        catch (Exception ex)
        {
            throw CreateCommandException("Communication failure.", ex, writeBuf);
        }
    }

    protected byte[] SendCommand(byte command, ReadOnlySpan<byte> data)
    {
        var readBuf = _dataWriter.CreateHidPacketBuffer();
        var readBufData = readBuf.AsSpan(1);
        var writeBufData = _dataWriter.CreateCommandPacket(command, _sequenceCounter.Next(), data);
        var writeBuf = _dataWriter.CreateHidPacket(writeBufData);
        byte readCrcByte, readBufCrcResult;

        try
        {
            WriteAndRead(writeBuf, readBuf);
            readCrcByte = _dataReader.GetChecksumByte(readBufData);
            readBufCrcResult = _dataReader.CalculateChecksumByte(readBufData);
        }
        catch (Exception ex)
        {
            _rebootManager.NotifyReadFailure();
            throw CreateCommandException("Communication failure.", ex, writeBuf, readBuf);
        }

        if (readCrcByte != readBufCrcResult)
        {
            _rebootManager.NotifyReadFailure();
            var checksumEx = CreateCommandException("Checksum failure.", default, writeBuf, readBuf);
            LogWarning(checksumEx);
        }
        else
        {
            _rebootManager.NotifyReadSuccess();
        }

        return readBufData.ToArray();
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
        Write(writeBuffer);
        _device.Read(readBuffer);

        if (CanLogDebug)
        {
            LogDebug($"READ:  {readBuffer.ToHexString()}");
        }
    }

    private void Write(byte[] writeBuffer)
    {
        if (_firstWrite)
        {
            _firstWrite = false;
            Utils.SyncWait(DEVICE_INITIAL_WRITE_DELAY_MS);
        }

        if (CanLogDebug)
        {
            LogDebug($"WRITE: {writeBuffer.ToHexString()}");
        }

        var useDelay = true;

        try
        {
            _deviceMetrics.WriteStart();
            _device.Write(writeBuffer);
        }
        catch
        {
            useDelay = false;
        }
        finally
        {
            var delay = (int)_deviceMetrics.WriteEnd();

            if (CanLogDebug)
            {
                LogDebug($"Hydro Platinum Device Metrics: delay = {delay} ms");
            }

            if (useDelay)
            {
                Utils.SyncWait(delay);
            }
        }
    }

    private void WriteDirect(byte[] packet)
    {
        if (CanLogDebug)
        {
            LogDebug($"WRITE_DIRECT: {packet.ToHexString()}");
        }

        _device.WriteDirect(packet);
    }

    private void RebootManager_RebootRequired(object sender, EventArgs e)
    {
        RebootDevice();
    }

    private void RebootManager_RebootSuccessful(object sender, EventArgs e)
    {
        // after device reboots, lighting might not be in direct mode for some zones
        _resetEnableDirectLightingOnNextRefresh = true;
    }

    private void RebootDevice()
    {
        bool RebootImpl()
        {
            LogInfo("Rebooting device");

            using (_guardManager.AwaitExclusiveAccess())
            {
                try
                {
                    // tell the device to reboot its EFM8 microcontroller
                    var rebootPacket = _dataWriter.CreateRebootPacket();
                    try
                    {
                        WriteDirect(_dataWriter.CreateHidPacket(rebootPacket));
                    }
                    catch
                    {
                        // ignore
                        // if reboot successful, the reboot will cause this operation to "fail"
                    }
                    LogInfo("Sent reboot packet");

                    LogInfo("Closing device");
                    _device.Close();

                    LogInfo("Waiting to reopen device");
                    Utils.SyncWait(DEVICE_POST_REBOOT_WAIT_MS);

                    LogInfo("Opening device");
                    var (opened, ex) = _device.Open();
                    if (!opened)
                    {
                        LogWarning("Failed to reopen device after reboot");
                        if (ex is not null)
                        {
                            LogError(ex);
                        }
                        return false;
                    }

                    LogInfo("Attempting to read");
                    _ = ReadState();
                    LogInfo("Device reboot successful");

                    return true;
                }
                catch (Exception ex)
                {
                    LogWarning("Failed to reboot device");
                    LogError(ex);

                    return false;
                }
            }
        }

        var lockTaken = false;

        try
        {
            Monitor.TryEnter(_refreshLock, 2000, ref lockTaken);
            if (lockTaken)
            {
                var rebooted = RebootImpl();
                if (!rebooted)
                {
                    _rebootManager.NotifyRebootFailure();
                }
                else
                {
                    _rebootManager.NotifyRebootSuccess();
                }
            }
            else
            {
                LogWarning("Failed to acquire refresh lock for device reboot");
                _rebootManager.NotifyRebootFailure();
            }
        }
        finally
        {
            if (lockTaken)
            {
                Monitor.Exit(_refreshLock);
            }
        }
    }

    private void ResetEnableDirectLighting()
    {
        LogInfo("Reset direct lighting START");

        try
        {
            // disable
            var directLightingPacketDisable = _dataWriter.CreateDirectLightingConfigurationPacket(_sequenceCounter.Next(), false, 0);
            WriteDirect(_dataWriter.CreateHidPacket(directLightingPacketDisable));
            Utils.SyncWait(50);

            // indexes
            var indexData = _dataWriter.CreateDefaultLightingIndexData().Chunk(40).ToList();
            for (int i = 0; i < indexData.Count; i++)
            {
                var packet = _dataWriter.CreateCommandPacket((byte)(Commands.LightingIndexes + i), _sequenceCounter.Next(), indexData[i], 0x00).ToArray();
                WriteDirect(_dataWriter.CreateHidPacket(packet));
                Utils.SyncWait(50);

                if (_sendOnlyFirstLightingIndexPacket)
                {
                    break;
                }
            }

            // colors
            var colorData = _dataWriter.CreateDefaultLightingColorData().Chunk(3 * 20).ToList();
            for (int i = 0; i < colorData.Count; i++)
            {
                var packet = _dataWriter.CreateCommandPacket((byte)(Commands.LightingColors + i), _sequenceCounter.Next(), colorData[i], 0x00).ToArray();
                WriteDirect(_dataWriter.CreateHidPacket(packet));
                Utils.SyncWait(50);
            }

            // enable
            var directLightingPacketEnable = _dataWriter.CreateDirectLightingConfigurationPacket(_sequenceCounter.Next(), true, 100);
            WriteDirect(_dataWriter.CreateHidPacket(directLightingPacketEnable));
            Utils.SyncWait(50);

            LogInfo("Reset direct lighting OK");
        }
        catch (Exception ex)
        {
            LogWarning("Reset direct lighting FAILED");
            LogWarning(ex);
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
}
