namespace Quraaa.Application.Shared.Exceptions
{
    public class UnauthenticatedException : Exception
    {
        public UnauthenticatedException()
            : base("Authentication credentials are invalid, expired, or revoked.")
        {
        }
    }
}
