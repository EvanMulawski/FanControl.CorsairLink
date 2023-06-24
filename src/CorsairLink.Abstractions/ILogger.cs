namespace CorsairLink;

public interface ILogger
{
    void Info(string category, string message);
    void Error(string category, string message);
    void Debug(string category, string message);
    void Flush();
    bool DebugEnabled { get; }
}