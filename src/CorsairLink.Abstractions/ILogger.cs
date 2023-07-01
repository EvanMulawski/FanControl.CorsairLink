namespace CorsairLink;

public interface ILogger
{
    void Info(string category, string message);
    void Error(string category, string message);
    void Error(string category, Exception exception);
    void Warning(string category, string message);
    void Warning(string category, Exception exception);
    void Debug(string category, string message);
    void Debug(string category, Exception exception);
    void Flush();
    bool DebugEnabled { get; }
}