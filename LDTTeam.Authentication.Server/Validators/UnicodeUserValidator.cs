using System.Linq;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api;
using Microsoft.AspNetCore.Identity;

namespace LDTTeam.Authentication.Server.Validators
{
    /// <summary>
    /// Custom user validator that allows Unicode letters and digits in usernames,
    /// fixing OAuth login issues with non-ASCII characters (e.g., æ, ö, ü).
    /// </summary>
    public class UnicodeUserValidator : UserValidator<ApplicationUser>
    {
        private static readonly char[] AllowedSpecialChars = { '.', '@', '-' };

        public override async Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
        {
            // Run base validation first (checks for null/empty username, etc.)
            var result = await base.ValidateAsync(manager, user);
            if (!result.Succeeded)
                return result;

            // Custom username character validation using Unicode-aware checks
            if (!string.IsNullOrEmpty(user.UserName))
            {
                var invalidChars = user.UserName
                    .Where(c => !char.IsLetterOrDigit(c) && !AllowedSpecialChars.Contains(c))
                    .Distinct()
                    .ToArray();

                if (invalidChars.Length > 0)
                {
                    return IdentityResult.Failed(new IdentityError
                    {
                        Code = "InvalidUserNameCharacters",
                        Description = $"Username '{user.UserName}' contains invalid characters: '{new string(invalidChars)}'. Only letters, digits, '.', '@', and '-' are allowed."
                    });
                }
            }

            return result;
        }
    }
}
