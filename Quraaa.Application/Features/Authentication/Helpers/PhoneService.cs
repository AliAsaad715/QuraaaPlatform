using PhoneNumbers;
using System.ComponentModel.DataAnnotations;

namespace IdentityServer.Helpers
{

    public interface IPhoneService
    {
        string? FormatToE164(string rawInput);
    }

    public class PhoneService : IPhoneService
    {
        private readonly PhoneNumberUtil _phoneUtil;

        public PhoneService()
        {
            _phoneUtil = PhoneNumberUtil.GetInstance();
        }
        public string? FormatToE164(string rawInput)
        {
            if (string.IsNullOrWhiteSpace(rawInput)) return null;
            if (!rawInput.Trim().StartsWith("+"))
            {
                return null;
            }

            try
            {
                // Passing 'null' as the region forces the library to look for the code in the string
                var number = _phoneUtil.Parse(rawInput, null);

                // "IsValidNumber" checks if the length/prefix matches the country rules found in the string
                if (_phoneUtil.IsValidNumber(number))
                {
                    return _phoneUtil.Format(number, PhoneNumberFormat.E164);
                }
            }
            catch (NumberParseException)
            {
                // Input format was completely wrong
                throw new ValidationException("phone format was fully wrong");
            }

            return null; // Or throw exception depending on your flow
        }
    }
}
