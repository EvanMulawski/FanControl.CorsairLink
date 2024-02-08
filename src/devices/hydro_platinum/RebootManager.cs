namespace CorsairLink.Devices.HydroPlatinum;

internal sealed class RebootManager
{
    private readonly int _maxReadFailuresBeforeReboot;

    private int _readFailureCount;
    private bool _canFire = true;

    public event EventHandler? RebootRequired;
    public event EventHandler? RebootSuccessful;

    public RebootManager(int maxReadFailuresBeforeReboot)
    {
        _maxReadFailuresBeforeReboot = maxReadFailuresBeforeReboot;
    }

    public void NotifyReadFailure()
    {
        ++_readFailureCount;

        if (_canFire && _readFailureCount >= _maxReadFailuresBeforeReboot)
        {
            TriggerReboot();
        }
    }

    public void NotifyReadSuccess()
    {
        _readFailureCount = 0;
        _canFire = true;
    }

    public void NotifyRebootFailure()
    {
        _canFire = true;
    }

    public void NotifyRebootSuccess()
    {
        RebootSuccessful?.Invoke(this, EventArgs.Empty);
    }

    internal void TriggerReboot()
    {
        _canFire = false;
        RebootRequired?.Invoke(this, EventArgs.Empty);
    }
}
