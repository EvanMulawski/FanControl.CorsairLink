namespace CorsairLink.Tests
{
    internal sealed class FakeLogger : ILogger
    {
        public bool DebugEnabled => false;

        public void Debug(string deviceName, string message)
        {

        }

        public void Error(string deviceName, string message)
        {

        }

        public void Log(string message)
        {

        }

        public void Info(string deviceName, string message)
        {

        }

        public void Flush()
        {

        }
    }
}
