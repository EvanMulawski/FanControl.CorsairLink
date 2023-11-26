﻿using CorsairLink.Devices.ICueLink;

namespace CorsairLink.Tests;

public class LinkHubDataReaderTests
{
    [Fact]
    public void GetDevices_ReturnsAllSubDevices__1()
    {
        // Arrange
        var data = TestUtils.ParseHexString("0000080021000b000001000000051a3031303032433843333230333537423732423030303044344334000001000000051a303130303238324638323033353832424642303030314441363200000000000000000000000000000000000000000000000000000000000000000000000000000000000001000000051a3031303030333835343230333534324233453030303034344234000001000000051a3031303031333630333230333538393845393030303037433830000007000000051a3032333245413031303045353931354430334132454230303030000001000000051a3031303030454141353230333533464632413030303136364445000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");

        // Act
        var devices = LinkHubDataReader.GetDevices(data);

        // Assert
        Assert.Equal(6, devices.Count);
        Assert.Equal(8, devices.ElementAt(2).Channel);
        Assert.Equal(0x07, devices.ElementAt(4).Type);
        Assert.Equal(0x00, devices.ElementAt(4).Model);
    }

    [Fact]
    public void GetDevices_ReturnsAllSubDevices__2()
    {
        // Arrange
        var data = TestUtils.ParseHexString("0000080021000c000001000000051a3031303033423432393230333536344144383030303232334234000001000000051a3031303033423432393230333536344144383030303143423042000001000000051a30313030323832463832303335383242464230303031414439310000000000000000000000000000000000000000000000000000000000000000000001000000051a3031303032383246383230333538324246423030303239393932000001000000051a3031303032383246383230333538324246423030303238343043000007050000051a3032353235303136303046364142363130333442383830303030000001000000051a3031303032383246383230333538324246423030303238383030000001000000051a3031303030334137323230333531383931443030303134433632000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");

        // Act
        var devices = LinkHubDataReader.GetDevices(data);

        // Assert
        Assert.Equal(8, devices.Count);
        Assert.Equal(8, devices.ElementAt(3).Channel);
        Assert.Equal(0x07, devices.ElementAt(5).Type);
        Assert.Equal(0x05, devices.ElementAt(5).Model);
    }

    [Fact]
    public void GetFirmwareVersion_ReturnsHumanReadableVersionString()
    {
        // Arrange
        var data = TestUtils.ParseHexString("00000200010650010001000000051a3031303033423432393230333536344144383030303232334234000001000000051a3031303033423432393230333536344144383030303143423042000001000000051a30313030323832463832303335383242464230303031414439310000000000000000000000000000000000000000000000000000000000000000000001000000051a3031303032383246383230333538324246423030303239393932000001000000051a3031303032383246383230333538324246423030303238343043000007050000051a3032353235303136303046364142363130333442383830303030000001000000051a3031303032383246383230333538324246423030303238383030000001000000051a3031303030334137323230333531383931443030303134433632000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");

        // Act
        var firmwareVersion = LinkHubDataReader.GetFirmwareVersion(data);

        // Assert
        Assert.Equal("1.6.336", firmwareVersion);
    }

    [Fact]
    public void GetSpeedSensors_ReturnsAllSpeedSensors()
    {
        // Arrange
        var data = TestUtils.ParseHexString("0000080025000F01000000E20100520300E401010000010000010000010000002203001502009A0A00210300CF0100B901010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");

        // Act
        var sensors = LinkHubDataReader.GetSpeedSensors(data);

        // Assert
        Assert.Equal(15, sensors.Count);
        Assert.Equal(0, sensors.ElementAt(0).Channel);
        Assert.Equal(LinkHubSpeedSensorStatus.Unavailable, sensors.ElementAt(0).Status);
        Assert.Equal(default, sensors.ElementAt(0).Rpm);
        Assert.Equal(1, sensors.ElementAt(1).Channel);
        Assert.Equal(LinkHubSpeedSensorStatus.Available, sensors.ElementAt(1).Status);
        Assert.Equal(482, sensors.ElementAt(1).Rpm);
    }

    [Fact]
    public void GetTemperatureSensors_ReturnsAllTemperatureSensors()
    {
        // Arrange
        var data = TestUtils.ParseHexString("0000080010000F01000000D00000CD0000C90001000001000001000001000000F600001F01003901001401003201001501010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");

        // Act
        var sensors = LinkHubDataReader.GetTemperatureSensors(data);

        // Assert
        Assert.Equal(15, sensors.Count);
        Assert.Equal(0, sensors.ElementAt(0).Channel);
        Assert.Equal(LinkHubTemperatureSensorStatus.Unavailable, sensors.ElementAt(0).Status);
        Assert.Equal(default, sensors.ElementAt(0).TempCelsius);
        Assert.Equal(1, sensors.ElementAt(1).Channel);
        Assert.Equal(LinkHubTemperatureSensorStatus.Available, sensors.ElementAt(1).Status);
        Assert.Equal(20.8f, sensors.ElementAt(1).TempCelsius!.Value, 0.1f);
    }
}
