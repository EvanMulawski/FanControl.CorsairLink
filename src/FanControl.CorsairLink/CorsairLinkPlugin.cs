namespace FanControl.CorsairLink;

using FanControl.Plugins;

public class CorsairLinkPlugin : IPlugin2
{
    string IPlugin.Name => "CorsairLink";

    void IPlugin.Close()
    {
        throw new NotImplementedException();
    }

    void IPlugin.Initialize()
    {
        throw new NotImplementedException();
    }

    void IPlugin.Load(IPluginSensorsContainer _container)
    {
        throw new NotImplementedException();
    }

    void IPlugin2.Update()
    {
        throw new NotImplementedException();
    }
}
