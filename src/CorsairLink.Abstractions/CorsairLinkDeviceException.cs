namespace CorsairLink
{
    [Serializable]
    public sealed class CorsairLinkDeviceException : Exception
    {
        public CorsairLinkDeviceException()
        {
        }

        public CorsairLinkDeviceException(string message) : base(message)
        {
        }

        public CorsairLinkDeviceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}