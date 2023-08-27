namespace CorsairLink.Tests
{
    internal sealed class FakeLogger : ILogger
    {
        public bool DebugEnabled => false;

        public void Debug(string deviceName, string message)
        {

        }

        public void Debug(string category, Exception exception)
        {

        }

        public void Debug(string category, string message, Exception exception)
        {

        }

        public void Warning(string deviceName, string message)
        {

        }

        public void Warning(string category, Exception exception)
        {

        }

        public void Warning(string category, string message, Exception exception)
        {

        }

        public void Error(string deviceName, string message)
        {

        }

        public void Error(string category, Exception exception)
        {

        }

        public void Error(string category, string message, Exception exception)
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
