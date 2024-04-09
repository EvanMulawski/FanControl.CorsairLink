namespace CorsairLink.Tests
{
    internal sealed class TestDeviceProxy : IHidDeviceProxy
    {
        private readonly Queue<byte[]> _responseQueue;

        public TestDeviceProxy(params byte[][] communicationSequence)
        {
            _responseQueue = new Queue<byte[]>(communicationSequence);
        }

        public void ClearEnqueuedReports()
        {

        }

        public void Close()
        {

        }

        public HidDeviceInfo GetDeviceInfo()
        {
            return new HidDeviceInfo("\\\\test", 0, 0, "Test", "1234567890");
        }

        public void OnReconnect(Action? reconnectAction)
        {

        }

        public (bool Opened, Exception? Exception) Open()
        {
            return (true, default);
        }

        public void Read(byte[] buffer)
        {
            var response = _responseQueue.Dequeue();
            if (response is null)
            {
                return;
            }

            response.CopyTo(buffer, 0);
        }

        public void Write(byte[] buffer)
        {

        }

        public void WriteDirect(byte[] buffer)
        {

        }
    }

    internal sealed class TestGuardManager : IDeviceGuardManager
    {
        public IDisposable AwaitExclusiveAccess()
        {
            return new _Disposable();
        }

        private sealed class _Disposable : IDisposable
        {
            public void Dispose()
            {

            }
        }
    }
}
