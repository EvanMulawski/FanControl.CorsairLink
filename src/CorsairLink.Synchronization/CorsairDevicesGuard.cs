using System.Security.AccessControl;
using System.Security.Principal;

namespace CorsairLink.Synchronization;

internal sealed class CorsairDevicesGuard
{
    public const string MutexName = "Global\\CorsairLinkReadWriteGuardMutex";
    private static readonly Mutex _mutex = CreateMutex();

    public static void Acquire()
    {
        while (true)
        {
            try
            {
                _mutex.WaitOne();
                break;
            }
            catch (AbandonedMutexException)
            {
                _mutex.ReleaseMutex();
            }
        }
    }

    public static void Release()
    {
        _mutex.ReleaseMutex();
    }

    private static Mutex CreateMutex()
    {
        var mutexSecurity = new MutexSecurity();
        mutexSecurity.AddAccessRule(new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow));
        var mutex = new Mutex(false, MutexName);
        mutex.SetAccessControl(mutexSecurity);
        return mutex;
    }
}

