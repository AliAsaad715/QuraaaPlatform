using Quraaa.Domain.User.Enums;

namespace Quraaa.API.Requests.Authentication
{
    public class RegisterRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public Gender Gender { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public List<Guid> Interests { get; set; } = new();
    }
}
