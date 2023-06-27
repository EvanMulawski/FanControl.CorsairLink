using System;
using System.Runtime.InteropServices;

namespace CorsairLink.SiUsbXpress.Driver;

public static class SiUsbXpressDriver
{
    private const string DLL_NAME = "SiUSBXp.dll";

    public const byte SI_RETURN_SERIAL_NUMBER = 0;
    public const byte SI_RETURN_DESCRIPTION = 1;
    public const byte SI_RETURN_LINK_NAME = 2;
    public const byte SI_RETURN_VID = 3;
    public const byte SI_RETURN_PID = 4;
    public const int SI_MAX_DEVICE_STRLEN = 256;

    [DllImport(DLL_NAME)]
    public static extern SI_STATUS SI_GetNumDevices(ref uint lpdwNumDevices);

    [DllImport(DLL_NAME)]
    public static extern SI_STATUS SI_GetProductString(
        uint dwDeviceNum,
        byte[] lpvDeviceString,
        uint dwFlags);

    [DllImport(DLL_NAME)]
    public static extern SI_STATUS SI_Open(
        uint dwDevice,
        ref IntPtr cyHandle);

    [DllImport(DLL_NAME)]
    public static extern SI_STATUS SI_Close(IntPtr cyHandle);

    [DllImport(DLL_NAME)]
    public static extern SI_STATUS SI_Read(
        IntPtr cyHandle,
        byte[] lpBuffer,
        uint dwBytesToRead,
        ref uint lpdwBytesReturned,
        IntPtr o);

    [DllImport(DLL_NAME)]
    public static extern SI_STATUS SI_Write(
        IntPtr cyHandle,
        byte[] lpBuffer,
        uint dwBytesToWrite,
        ref uint lpdwBytesWritten,
        IntPtr o);

    [DllImport(DLL_NAME)]
    public static extern SI_STATUS SI_DeviceIOControl(
        IntPtr cyHandle,
        uint dwIoControlCode,
        byte[] lpInBuffer,
        uint dwBytesToRead,
        byte[] lpOutBuffer,
        uint dwBytesToWrite,
        ref uint lpdwBytesSucceeded);

    [DllImport(DLL_NAME)]
    public static extern SI_STATUS SI_FlushBuffers(
        IntPtr cyHandle,
        byte FlushTransmit,
        byte FlushReceive);

    [DllImport(DLL_NAME)]
    public static extern SI_STATUS SI_SetTimeouts(
        uint dwReadTimeout,
        uint dwWriteTimeout);

    [DllImport(DLL_NAME)]
    public static extern SI_STATUS SI_GetTimeouts(
        ref uint lpdwReadTimeout,
        ref uint lpdwWriteTimeout);

    [DllImport(DLL_NAME)]
    public static extern SI_STATUS SI_CheckRXQueue(
        IntPtr cyHandle,
        ref uint lpdwNumBytesInQueue,
        ref uint lpdwQueueStatus);

    [DllImport(DLL_NAME)]
    public static extern SI_STATUS SI_SetBaudRate(
        IntPtr cyHandle,
        uint dwBaudRate);

    [DllImport(DLL_NAME)]
    public static extern SI_STATUS SI_SetBaudDivisor(
        IntPtr cyHandle,
        ushort wBaudDivisor);

    [DllImport(DLL_NAME)]
    public static extern SI_STATUS SI_SetLineControl(
        IntPtr cyHandle,
        ushort wLineControl);

    [DllImport(DLL_NAME)]
    public static extern SI_STATUS SI_SetFlowControl(
        IntPtr cyHandle,
        byte bCTS_MaskCode,
        byte bRTS_MaskCode,
        byte bDTR_MaskCode,
        byte bDSR_MaskCode,
        byte bDCD_MaskCode,
        bool bFlowXonXoff);

    [DllImport(DLL_NAME)]
    public static extern SI_STATUS SI_GetModemStatus(
        IntPtr cyHandle,
        ref byte ModemStatus);

    [DllImport(DLL_NAME)]
    public static extern SI_STATUS SI_SetBreak(
        IntPtr cyHandle,
        ushort wBreakState);

    [DllImport(DLL_NAME)]
    public static extern SI_STATUS SI_ReadLatch(
        IntPtr cyHandle,
        ref byte lpbLatch);

    [DllImport(DLL_NAME)]
    public static extern SI_STATUS SI_WriteLatch(
        IntPtr cyHandle,
        byte bMask,
        byte bLatch);

    [DllImport(DLL_NAME)]
    public static extern SI_STATUS SI_GetPartNumber(
        IntPtr cyHandle,
        ref byte lpbPartNum);

    [DllImport(DLL_NAME)]
    public static extern SI_STATUS SI_GetDeviceProductString(
        IntPtr cyHandle,
        byte[] lpProduct,
        ref byte lpbLength,
        bool bConvertToASCII);

    [DllImport(DLL_NAME)]
    public static extern SI_STATUS SI_GetDLLVersion(
        ref uint HighVersion,
        ref uint LowVersion);

    [DllImport(DLL_NAME)]
    public static extern SI_STATUS SI_GetDriverVersion(
        ref uint HighVersion,
        ref uint LowVersion);

    public enum SI_STATUS : byte
    {
        SI_SUCCESS = 0x00,
        SI_INVALID_HANDLE = 0x01,
        SI_READ_ERROR = 0x02,
        SI_RX_QUEUE_NOT_READY = 0x03,
        SI_WRITE_ERROR = 0x04,
        SI_RESET_ERROR = 0x05,
        SI_INVALID_PARAMETER = 0x06,
        SI_INVALID_REQUEST_LENGTH = 0x07,
        SI_DEVICE_IO_FAILED = 0x08,
        SI_INVALID_BAUDRATE = 0x09,
        SI_FUNCTION_NOT_SUPPORTED = 0x0A,
        SI_GLOBAL_DATA_ERROR = 0x0B,
        SI_SYSTEM_ERROR_CODE = 0x0C,
        SI_READ_TIMED_OUT = 0x0D,
        SI_WRITE_TIMED_OUT = 0x0E,
        SI_IO_PENDING = 0x0F,
        SI_DEVICE_NOT_FOUND = 0xFF,
    }
}
