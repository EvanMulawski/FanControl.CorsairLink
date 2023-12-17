using System.Buffers.Binary;

namespace CorsairLink.Devices.CommanderCore;

public static class CommanderCoreDataReader
{
    public static string GetFirmwareVersion(ReadOnlySpan<byte> packet)
    {
        var v1 = (int)packet[3];
        var v2 = (int)packet[4];
        var v3 = BinaryPrimitives.ReadInt16LittleEndian(packet.Slice(5, 2));

        return $"{v1}.{v2}.{v3}";
    }

    public static byte GetSpeedSensorCount(ReadOnlySpan<byte> packet)
    {
        return packet[5];
    }

    public static IReadOnlyCollection<CommanderCoreSpeedSensor> GetSpeedSensors(ReadOnlySpan<byte> connectedSpeedsPacket, ReadOnlySpan<byte> speedsPacket)
    {
        var count = GetSpeedSensorCount(connectedSpeedsPacket);
        var connectedData = connectedSpeedsPacket.Slice(6);
        var sensorData = speedsPacket.Slice(6);
        var sensors = new List<CommanderCoreSpeedSensor>(count);

        for (int i = 0, s = 0; i < count; i++, s += 2)
        {
            var currentSensor = sensorData.Slice(s, 2);
            var status = (CommanderCoreSpeedSensorStatus)connectedData[i];
            int? rpm = status == CommanderCoreSpeedSensorStatus.Available
                ? BinaryPrimitives.ReadInt16LittleEndian(currentSensor)
                : null;

            sensors.Add(new CommanderCoreSpeedSensor(i, status, rpm));
        }

        return sensors;
    }

    public static IReadOnlyCollection<CommanderCoreTemperatureSensor> GetTemperatureSensors(ReadOnlySpan<byte> packet)
    {
        var count = packet[5];
        var sensorData = packet.Slice(6);
        var sensors = new List<CommanderCoreTemperatureSensor>(count);

        for (int i = 0, s = 0; i < count; i++, s += 3)
        {
            var currentSensor = sensorData.Slice(s, 3);
            var status = (CommanderCoreTemperatureSensorStatus)currentSensor[0];
            float? tempCelsius = status == CommanderCoreTemperatureSensorStatus.Available
                ? BinaryPrimitives.ReadInt16LittleEndian(currentSensor.Slice(1, 2)) / 10f
                : null;

            sensors.Add(new CommanderCoreTemperatureSensor(i, status, tempCelsius));
        }

        return sensors;
    }
}
