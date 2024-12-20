using System.Buffers.Binary;
using System.Text;

namespace CorsairLink.Devices.ICueLink;

public static class LinkHubDataReader
{
    public static IReadOnlyCollection<LinkHubConnectedDevice> GetDevices(ReadOnlySpan<byte> subDevicesPacket, ReadOnlySpan<byte> subDevicesContinuationPacket = default)
    {
        static IReadOnlyCollection<LinkHubConnectedDevice> ParsePacket(ReadOnlySpan<byte> packetData)
        {
            // payload format:
            // [0] = last channel index
            // [1,] = devices

            // device format:
            // [2] = device type
            // [3] = device model
            // [7] = device id length
            // (if zeroed, channel is empty)
            // [8] first byte of device id (device id consists of ASCII character codes)
            // next channel starts in (device id length)+8 bytes

            // channels start at 0x01 (not 0x00)

            var devices = new List<LinkHubConnectedDevice>();

            if (packetData.Length == 0)
            {
                return devices;
            }

            var lastChannel = packetData[0];
            var d = packetData.Slice(1);
            var i = 0;

            for (int ch = 1; ch <= lastChannel; ch++)
            {
                var deviceIdLength = d[i + 7];
                if (deviceIdLength == 0)
                {
                    i += 8;
                    continue;
                }

                var deviceInfo = d.Slice(i, 8);
                var isPacketEnd = i + 8 + deviceIdLength > d.Length;
                var deviceId = isPacketEnd ? d.Slice(i + 8) : d.Slice(i + 8, deviceIdLength);

                var device = new LinkHubConnectedDevice(
                    channel: ch,
                    id: Encoding.ASCII.GetString(deviceId.ToArray()),
                    type: deviceInfo[2],
                    model: deviceInfo[3]);

                devices.Add(device);

                if (isPacketEnd)
                {
                    break;
                }

                i += (8 + deviceIdLength);
            }

            return devices;
        }

        var subDevicesPacketData1 = subDevicesPacket.Slice(6);
        var subDevicesPacketData2 = subDevicesContinuationPacket.Length > 4 ? subDevicesContinuationPacket.Slice(4) : [];

        var subDevicesFullPacketData = new byte[subDevicesPacketData1.Length + subDevicesPacketData2.Length];
        subDevicesPacketData1.CopyTo(subDevicesFullPacketData);
        subDevicesPacketData2.CopyTo(subDevicesFullPacketData.AsSpan(subDevicesPacketData1.Length));

        var devices = ParsePacket(subDevicesFullPacketData);

        return devices;
    }

    public static string GetFirmwareVersion(ReadOnlySpan<byte> packet)
    {
        var v1 = (int)packet[4];
        var v2 = (int)packet[5];
        var v3 = BinaryPrimitives.ReadInt16LittleEndian(packet.Slice(6, 2));

        return $"{v1}.{v2}.{v3}";
    }

    public static IReadOnlyCollection<LinkHubSpeedSensor> GetSpeedSensors(ReadOnlySpan<byte> packet)
    {
        var count = packet[6];
        var sensorData = packet.Slice(7);
        var sensors = new List<LinkHubSpeedSensor>(count);

        for (int i = 0, s = 0; i < count; i++, s += 3)
        {
            var currentSensor = sensorData.Slice(s, 3);
            var status = (LinkHubSpeedSensorStatus)currentSensor[0];
            int? rpm = status == LinkHubSpeedSensorStatus.Available
                ? BinaryPrimitives.ReadInt16LittleEndian(currentSensor.Slice(1, 2))
                : null;

            sensors.Add(new LinkHubSpeedSensor(i, status, rpm));
        }

        return sensors;
    }

    public static IReadOnlyCollection<LinkHubTemperatureSensor> GetTemperatureSensors(ReadOnlySpan<byte> packet)
    {
        var count = packet[6];
        var sensorData = packet.Slice(7);
        var sensors = new List<LinkHubTemperatureSensor>(count);

        for (int i = 0, s = 0; i < count; i++, s += 3)
        {
            var currentSensor = sensorData.Slice(s, 3);
            var status = (LinkHubTemperatureSensorStatus)currentSensor[0];
            float? tempCelsius = status == LinkHubTemperatureSensorStatus.Available
                ? BinaryPrimitives.ReadInt16LittleEndian(currentSensor.Slice(1, 2)) / 10f
                : null;

            sensors.Add(new LinkHubTemperatureSensor(i, status, tempCelsius));
        }

        return sensors;
    }
}
