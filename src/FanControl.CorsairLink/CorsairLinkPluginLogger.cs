using CorsairLink;
using FanControl.Plugins;

namespace FanControl.CorsairLink;

internal class CorsairLinkPluginLogger : ILogger
{
    private readonly IPluginLogger _pluginLogger;
    private readonly bool _debugEnabled;

    public CorsairLinkPluginLogger(IPluginLogger pluginLogger)
    {
        _pluginLogger = pluginLogger;
        _debugEnabled = Utils.GetEnvironmentFlag("FANCONTROL_CORSAIRLINK_DEBUG_LOGGING_ENABLED");
    }

    public void Debug(string deviceName, string message)
    {
        if (!_debugEnabled)
        {
            return;
        }

        Log($"(debug) {deviceName}: {message}");
    }

    public void Error(string deviceName, string message) => Log($"(error) {deviceName}: {message}");

    public void Normal(string deviceName, string message) => Log($"{deviceName}: {message}");

    public void Log(string message) => _pluginLogger.Log($"[CorsairLink] {message}");
}
