using Microsoft.AspNetCore.Identity;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public IdentityService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<bool> IsPhoneNumberUniqueAsync(string phoneNumber)
        {
            var user = await _userManager.FindByNameAsync(phoneNumber);
            return user == null;
        }

        public async Task<string> CreateUserIdentityAsync(Guid id, string phoneNumber, string password)
        {
            var identityUser = new ApplicationUser
            {
                Id = id,
                UserName = phoneNumber,
                PhoneNumber = phoneNumber,
                Email = $"{phoneNumber}@quraaa.com",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };

            var result = await _userManager.CreateAsync(identityUser, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Failed to create user in identity system: {errors}");
            }

            return identityUser.PasswordHash!;
        }
    }
}
