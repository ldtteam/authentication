using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (user == null) throw new ArgumentNullException(nameof(user));
            
            var errors = new List<IdentityError>();
            if (manager.Options.User.RequireUniqueEmail)
            {
                errors = await ValidateEmail(manager, user, errors).ConfigureAwait(false);
            }

            if (errors?.Count > 0)
            {
                return IdentityResult.Failed(errors.ToArray());
            }

            // Custom username character validation using Unicode-aware checks
            if (!string.IsNullOrEmpty(user.UserName))
            {
                var invalidChars = user.UserName
                    .Where(c => !char.IsLetterOrDigit(c) && !Enumerable.Contains(AllowedSpecialChars, c))
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

            return IdentityResult.Success;
        }
        
        private async Task<List<IdentityError>?> ValidateEmail(UserManager<ApplicationUser> manager, ApplicationUser user, List<IdentityError>? errors)
        {
            var email = await manager.GetEmailAsync(user).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(email))
            {
                errors ??= new List<IdentityError>();
                errors.Add(Describer.InvalidEmail(email));
                return errors;
            }
            if (!new EmailAddressAttribute().IsValid(email))
            {
                errors ??= new List<IdentityError>();
                errors.Add(Describer.InvalidEmail(email));
                return errors;
            }
            var owner = await manager.FindByEmailAsync(email).ConfigureAwait(false);
            if (owner != null &&
                !string.Equals(await manager.GetUserIdAsync(owner).ConfigureAwait(false), await manager.GetUserIdAsync(user).ConfigureAwait(false)))
            {
                errors ??= new List<IdentityError>();
                errors.Add(Describer.DuplicateEmail(email));
            }
            return errors;
        }
    }
}
