using Quraaa.Domain.User.Enums;

namespace Quraaa.API.Requests.Profiles
{
    public class UpdateProfileRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public Gender Gender { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public string? ProfileImageUrl { get; set; }
        public List<string> Interests { get; set; } = new();
    }
}
