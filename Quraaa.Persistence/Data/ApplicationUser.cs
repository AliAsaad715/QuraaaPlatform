using Microsoft.AspNetCore.Identity;

namespace Quraaa.Persistence.Data
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }
    }
}
