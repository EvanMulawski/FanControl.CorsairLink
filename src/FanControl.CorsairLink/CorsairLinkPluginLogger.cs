using CorsairLink;
using FanControl.Plugins;

namespace FanControl.CorsairLink;

internal class CorsairLinkPluginLogger : ILogger
{
    private readonly IPluginLogger _pluginLogger;

    public CorsairLinkPluginLogger(IPluginLogger pluginLogger)
    {
        _pluginLogger = pluginLogger;
    }

    public void Log(string message) => _pluginLogger.Log(message);
}
