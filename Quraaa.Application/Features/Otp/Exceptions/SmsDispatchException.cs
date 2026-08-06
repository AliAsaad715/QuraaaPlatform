namespace Quraaa.Application.Features.Otp.Exceptions
{
    public enum SmsDispatchOutcome
    {
        Unknown,
        DefinitelyNotDispatched
    }

    public sealed class SmsDispatchException : Exception
    {
        public SmsDispatchException(
            string message,
            SmsDispatchOutcome outcome,
            Exception innerException) : base(message, innerException)
        {
            Outcome = outcome;
        }

        public SmsDispatchOutcome Outcome { get; }
    }
}
