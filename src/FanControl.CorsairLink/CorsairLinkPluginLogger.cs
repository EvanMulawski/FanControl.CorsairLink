using CorsairLink;
using FanControl.Plugins;

namespace FanControl.CorsairLink;

internal class CorsairLinkPluginLogger : ILogger
{
    private readonly IPluginLogger _pluginLogger;

    public bool DebugEnabled { get; }

    public CorsairLinkPluginLogger(IPluginLogger pluginLogger)
    {
        _pluginLogger = pluginLogger;
        DebugEnabled = Utils.GetEnvironmentFlag("FANCONTROL_CORSAIRLINK_DEBUG_LOGGING_ENABLED");
    }

    public void Debug(string deviceName, string message)
    {
        if (!DebugEnabled)
        {
            return;
        }

        Log($"(debug) {deviceName}: {message}");
    }

    public void Error(string deviceName, string message) => Log($"(error) {deviceName}: {message}");

    public void Normal(string deviceName, string message) => Log($"{deviceName}: {message}");

    public void Log(string message) => _pluginLogger.Log($"[CorsairLink] {message}");
}
