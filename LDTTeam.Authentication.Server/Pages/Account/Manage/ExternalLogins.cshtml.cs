using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Messages.User;
using LDTTeam.Authentication.Models.App.User;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.PatreonApiUtils.Messages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Wolverine;

namespace LDTTeam.Authentication.Server.Pages.Account.Manage
{
    [Authorize]
    public class ExternalLoginsModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IMessageBus bus)
        : PageModel
    {
        public IList<UserLoginInfo>? CurrentLogins { get; set; }

        public IList<AuthenticationScheme>? OtherLogins { get; set; }

        public bool ShowRemoveButton { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        public Guid? MinecraftId { get; set; }

        public async Task<IActionResult> OnGetAsync(CancellationToken token)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Unable to load user with ID 'user.Id'.");
            }

            CurrentLogins = await userManager.GetLoginsAsync(user);
            OtherLogins = (await signInManager.GetExternalAuthenticationSchemesAsync())
                .Where(auth => CurrentLogins.All(ul => auth.Name != ul.LoginProvider))
                .ToList();
            ShowRemoveButton = user.PasswordHash != null || CurrentLogins.Count > 1;
            return Page();
        }

        public async Task<IActionResult> OnPostRemoveLoginAsync(string loginProvider, string providerKey)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID {User}.");
            }

            IdentityResult result = await userManager.RemoveLoginAsync(user, loginProvider, providerKey);
            if (!result.Succeeded)
            {
                StatusMessage = "The external login was not removed.";
                return RedirectToPage();
            }

            await signInManager.RefreshSignInAsync(user);
            StatusMessage = "The external login was removed.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostLinkLoginAsync(string provider)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            // Request a redirect to the external login provider to link a login for the current user
            string? redirectUrl = Url.Page("./ExternalLogins", "LinkLoginCallback");
            AuthenticationProperties properties =
                signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl,
                    userManager.GetUserId(User));
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetLinkLoginCallbackAsync(CancellationToken token)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID 'user.Id'.");
            }

            var info = await signInManager.GetExternalLoginInfoAsync(user.Id);
            if (info == null)
            {
                throw new InvalidOperationException(
                    $"Unexpected error occurred loading external login info for user with ID '{user.Id}'.");
            }

            IdentityResult result = await userManager.AddLoginAsync(user, info);
            if (!result.Succeeded && result.Errors.All(x => x.Code == "LoginAlreadyAssociated"))
            {
                var mergeUser = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (mergeUser == null)
                {
                    StatusMessage = "There was an error adding your external login";
                    return RedirectToPage();
                }
                
                IList<UserLoginInfo> mergeLogins = await userManager.GetLoginsAsync(mergeUser);
                IList<Claim> mergeClaims = await userManager.GetClaimsAsync(mergeUser);

                foreach (UserLoginInfo mergeLogin in mergeLogins)
                {
                    await userManager.RemoveLoginAsync(mergeUser, mergeLogin.LoginProvider, mergeLogin.ProviderKey);
                    await bus.PublishAsync(
                        new ExternalLoginDisconnectedFromUser(
                            Guid.Parse(mergeUser.Id),
                            Enum.Parse<AccountProvider>(mergeLogin.LoginProvider, true),
                            mergeLogin.ProviderKey
                        ));
                    
                    await userManager.AddLoginAsync(user, mergeLogin);
                    await bus.PublishAsync(
                        new ExternalLoginConnectedToUser(
                            Guid.Parse(user.Id),
                            Enum.Parse<AccountProvider>(mergeLogin.LoginProvider, true),
                            mergeLogin.ProviderKey
                        ));
                }

                await userManager.DeleteAsync(mergeUser);
                await bus.PublishAsync(new UserDeleted(Guid.Parse(mergeUser.Id)));
                
                if (info.Principal.Claims.All(c => c.Type != "patreon_membership_id") && mergeClaims.Any(claim => claim.Type == "patreon_membership_id"))
                {
                    var claim = mergeClaims.First(claim => claim.Type == "patreon_membership_id");
                    info.Principal.AddIdentity(new ClaimsIdentity([
                        new Claim("patreon_membership_id", claim.Value)
                    ]));
                }
                
                StatusMessage =
                    "The external login was already linked to an existing account, your accounts have been merged.";
            }
            else if (!result.Succeeded)
            {
                StatusMessage = "There was an error adding your external login";
                return RedirectToPage();
            }

            await bus.PublishAsync(new ExternalLoginConnectedToUser(
                Guid.Parse(user.Id), Enum.Parse<AccountProvider>(info.LoginProvider), info.ProviderKey
            ));

            if (info.Principal.Claims.Any(c => c.Type == "patreon_membership_id"))
            {
                await bus.PublishAsync(
                    new PatreonMembershipCreatedOrUpdated(
                        Guid.Parse(user.Id),
                        Guid.Parse(info.Principal.Claims.First(c => c.Type == "patreon_membership_id").Value)
                    )
                );
            }

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            await signInManager.RefreshSignInAsync(user);

            StatusMessage ??= "The external login was added.";
            return RedirectToPage();
        }
    }
}
