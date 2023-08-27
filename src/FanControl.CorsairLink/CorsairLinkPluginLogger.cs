using CorsairLink;
using System.Collections.Concurrent;
using System.Text;

namespace FanControl.CorsairLink;

internal sealed class CorsairLinkPluginLogger : ILogger
{
    private readonly FileLogger _logger;

    public event EventHandler<EventArgs>? ErrorLogged;

    public bool DebugEnabled { get; }

    public CorsairLinkPluginLogger()
    {
        _logger = new FileLogger("CorsairLink");
        DebugEnabled = Utils.GetEnvironmentFlag("FANCONTROL_CORSAIRLINK_DEBUG_LOGGING_ENABLED");
    }

    public void Debug(string category, string message)
    {
        if (!DebugEnabled)
        {
            return;
        }

        Log($"[DBG] {category}: {message}");
    }

    public void Debug(string category, Exception exception)
    {
        if (!DebugEnabled)
        {
            return;
        }

        Debug(category, exception.FormatForLogging());
    }

    public void Debug(string category, string message, Exception exception)
    {
        if (!DebugEnabled)
        {
            return;
        }

        Debug(category, FormatMessageWithException(message, exception));
    }

    public void Warning(string category, string message) => Log($"[WRN] {category}: {message}");

    public void Warning(string category, Exception exception) => Warning(category, exception.FormatForLogging());

    public void Warning(string category, string message, Exception exception) => Warning(category, FormatMessageWithException(message, exception));

    public void Error(string category, string message)
    {
        OnErrorLogged();
        Log($"[ERR] {category}: {message}");
    }

    public void Error(string category, Exception exception) => Error(category, exception.FormatForLogging());

    public void Error(string category, string message, Exception exception) => Error(category, FormatMessageWithException(message, exception));

    public void Info(string category, string message) => Log($"[INF] {category}: {message}");

    public void Flush() => _logger.Flush();

    private void Log(string message) => _logger.Log(message);

    private string FormatMessageWithException(string message, Exception exception)
        => string.Concat(message, Environment.NewLine, exception.FormatForLogging());

    private void OnErrorLogged()
    {
        ErrorLogged?.Invoke(this, EventArgs.Empty);
    }

    private sealed class FileLogger
    {
        private const long MAX_FILE_SIZE = 5000000L;
        private const string FILE_EXTENSION = ".log";

        private readonly string _logFileName;
        private readonly object _lock = new();
        private readonly ConcurrentQueue<string> _logs = new();

        public FileLogger(string logFileName)
        {
            _logFileName = logFileName + FILE_EXTENSION;
            var fileInfo = new FileInfo(_logFileName);
            var logFileNumber = 0;
            for (; fileInfo.Exists && fileInfo.Length > MAX_FILE_SIZE; fileInfo = new FileInfo(_logFileName))
            {
                _logFileName = logFileName + string.Format(".{0}", ++logFileNumber) + FILE_EXTENSION;
            }
        }

        public void Log(string message)
        {
            _logs.Enqueue($"{DateTime.UtcNow:O} {message}{Environment.NewLine}");
        }

        public void Flush()
        {
            bool lockTaken = false;

            try
            {
                Monitor.TryEnter(_lock, 1000, ref lockTaken);
                if (lockTaken)
                {
                    var sb = new StringBuilder();
                    var i = 0;
                    while (_logs.TryDequeue(out var log) && ++i < 1024)
                    {
                        sb.Append(log);
                    }

                    File.AppendAllText(_logFileName, sb.ToString(), Encoding.UTF8);
                }
                else
                {
                    throw new IOException("Unable to flush logs - timeout expired.");
                }
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(_lock);
                }
            }
        }
    }
}
