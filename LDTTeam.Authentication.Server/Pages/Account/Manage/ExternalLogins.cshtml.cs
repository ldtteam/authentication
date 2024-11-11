using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Api.Events;
using LDTTeam.Authentication.Modules.Api.Logging;
using LDTTeam.Authentication.Modules.Api.Utils;
using LDTTeam.Authentication.Modules.Minecraft.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Remora.Discord.API.Objects;

namespace LDTTeam.Authentication.Server.Pages.Account.Manage
{
    [Authorize]
    public class ExternalLoginsModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        MinecraftService minecraftService,
        IBackgroundEventsQueue eventsQueue,
        ILoggingQueue loggingQueue)
        : PageModel
    {
        public IList<UserLoginInfo>? CurrentLogins { get; set; }

        public IList<AuthenticationScheme>? OtherLogins { get; set; }

        public bool ShowRemoveButton { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        public Guid? MinecraftId { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = null!;

        public class InputModel
        {
            [Display(Name = "Minecraft Username")]
            public string? MinecraftUsername { get; init; }
        }

        public async Task<IActionResult> OnGetAsync(CancellationToken token)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Unable to load user with ID 'user.Id'.");
            }

            var minecraftId = User.Claims.FirstOrDefault(x => x.Type == "urn:minecraft:user:id")?.Value;
            if (minecraftId != null)
            {
                MinecraftId = new Guid(minecraftId);

                if (MinecraftId != null)
                {
                    var username = await minecraftService.GetUsernameFromUuid(MinecraftId.Value);
                    Input = new InputModel
                    {
                        MinecraftUsername = username
                    };
                }
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

        public async Task<IActionResult> OnPostLinkMinecraftAsync(CancellationToken token)
        {
            if (Input.MinecraftUsername == null)
                return await OnGetAsync(token);

            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Unable to load user with ID 'user.Id'.");
            }

            if ((await userManager.GetLoginsAsync(user)).Any(x =>
                x.LoginProvider.Equals("minecraft", StringComparison.InvariantCultureIgnoreCase)))
                return await OnGetAsync(token);

            var minecraftId = await minecraftService.GetUuidFromUsername(Input.MinecraftUsername, token);
            if (minecraftId == null)
            {
                StatusMessage = "Error Minecraft username was invalid";
                return await OnGetAsync(token);
            }

            await userManager.RemoveClaimsAsync(user,
                (await userManager.GetClaimsAsync(user)).Where(x => x.Type == "urn:minecraft:user:id"));
            await userManager.AddClaimAsync(user,
                new Claim("urn:minecraft:user:id", new Guid(minecraftId).ToString()));
            await signInManager.RefreshSignInAsync(user);

            List<EmbedField> fields =
            [
                new("User Name", user.UserName!, true),
                new("Minecraft User name", Input.MinecraftUsername, true),
                new("UUID", minecraftId, true)
            ];

            await loggingQueue.QueueBackgroundWorkItemAsync(new Embed
            {
                Title = "User linked minecraft UUID",
                Description = "A user has linked a new minecraft UUID to their account!",
                Colour = Color.Green,
                Fields = fields
            });

            return RedirectToPage("ExternalLogins");
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
                    await userManager.AddLoginAsync(user, mergeLogin);
                }

                if (!mergeLogins.Any(x =>
                    x.LoginProvider.Equals("minecraft", StringComparison.InvariantCultureIgnoreCase)))
                {
                    Claim? mergeClaim = mergeClaims.FirstOrDefault(x => x.Type == "urn:minecraft:user:id");
                    if (mergeClaim != null)
                        await userManager.AddClaimAsync(user, mergeClaim);
                }
                else
                {
                    IList<Claim> claims = await userManager.GetClaimsAsync(user);
                    Claim? mcClaim = claims.FirstOrDefault(x => x.Type == "urn:minecraft:user:id");
                    if (mcClaim != null)
                        await userManager.RemoveClaimAsync(user, mcClaim);
                }

                await userManager.DeleteAsync(mergeUser);
                
                StatusMessage =
                    "The external login was already linked to an existing account, your accounts have been merged.";
            }
            else if (!result.Succeeded)
            {
                StatusMessage = "There was an error adding your external login";
                return RedirectToPage();
            }

            List<EmbedField> fields =
            [
                new("User Name", user.UserName!, true),
                new("Provider", info.LoginProvider, true),
                new("Provider Key", info.ProviderKey, true)
            ];

            await loggingQueue.QueueBackgroundWorkItemAsync(new Embed
            {
                Title = "User linked new OAuth provider",
                Description = "A user has linked a new OAuth provider to their account!",
                Colour = Color.Green,
                Fields = fields
            });


            await eventsQueue.QueueBackgroundWorkItemAsync(async (events, scope, _) =>
            {
                await events._refreshContentEvent.InvokeAsync(scope, [info.LoginProvider]);
                await events._postRefreshContentEvent.InvokeAsync(scope);
            }, token);

            if (info.LoginProvider == "Minecraft")
            {
                await userManager.RemoveClaimsAsync(user,
                    (await userManager.GetClaimsAsync(user)).Where(x => x.Type == "urn:minecraft:user:id"));
            }

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            await signInManager.RefreshSignInAsync(user);

            StatusMessage ??= "The external login was added.";
            return RedirectToPage();
        }
    }
}
