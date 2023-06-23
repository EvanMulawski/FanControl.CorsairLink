namespace CorsairLink;

public interface ILogger
{
    void Log(string message);
    void Normal(string deviceName, string message);
    void Error(string deviceName, string message);
    void Debug(string deviceName, string message);
    bool DebugEnabled { get; }
}