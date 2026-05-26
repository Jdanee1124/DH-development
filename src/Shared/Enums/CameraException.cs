namespace CameraSDK.Exceptions
{
    public class CameraException : Exception
    {
        public int ErrorCode { get; }

        public CameraException(string message) : base(message) { }

        public CameraException(string message, int errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        public CameraException(string message, Exception innerException) : base(message, innerException) { }
    }
}
