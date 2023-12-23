namespace CorsairLink;

public interface IHidDeviceProxy
{
    HidDeviceInfo GetDeviceInfo();
    (bool Opened, Exception? Exception) Open();
    void Close();
    void Write(byte[] buffer);
    void WriteDirect(byte[] buffer);
    void Read(byte[] buffer);
    void ClearEnqueuedReports();
}
