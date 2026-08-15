namespace Quraaa.Application.Shared.Exceptions
{
    public sealed class FileStorageException : Exception
    {
        public FileStorageException(string message)
            : base(message)
        {
        }

        public FileStorageException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
