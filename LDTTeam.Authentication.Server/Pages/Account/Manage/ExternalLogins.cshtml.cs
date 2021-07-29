using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Security.Claims;
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
    public class ExternalLoginsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly MinecraftService _minecraftService;
        private readonly IBackgroundEventsQueue _eventsQueue;
        private readonly ILoggingQueue _loggingQueue;

        public ExternalLoginsModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            MinecraftService minecraftService, IBackgroundEventsQueue eventsQueue, ILoggingQueue loggingQueue)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _minecraftService = minecraftService;
            _eventsQueue = eventsQueue;
            _loggingQueue = loggingQueue;
        }

        public IList<UserLoginInfo> CurrentLogins { get; set; }

        public IList<AuthenticationScheme> OtherLogins { get; set; }

        public bool ShowRemoveButton { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public Guid? MinecraftId { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = null!;

        public class InputModel
        {
            [Display(Name = "Minecraft Username")]
            public string MinecraftUsername { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Unable to load user with ID 'user.Id'.");
            }

            string minecraftId = User.Claims.FirstOrDefault(x => x.Type == "urn:minecraft:user:id")?.Value;

            if (minecraftId != null)
            {
                MinecraftId = new Guid(minecraftId);

                if (MinecraftId != null)
                {
                    string username = await _minecraftService.GetUsernameFromUuid(MinecraftId.Value);
                    Input = new InputModel
                    {
                        MinecraftUsername = username
                    };
                }
            }

            CurrentLogins = await _userManager.GetLoginsAsync(user);
            OtherLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync())
                .Where(auth => CurrentLogins.All(ul => auth.Name != ul.LoginProvider))
                .ToList();
            ShowRemoveButton = user.PasswordHash != null || CurrentLogins.Count > 1;
            return Page();
        }

        public async Task<IActionResult> OnPostRemoveLoginAsync(string loginProvider, string providerKey)
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID 'user.Id'.");
            }

            IdentityResult result = await _userManager.RemoveLoginAsync(user, loginProvider, providerKey);
            if (!result.Succeeded)
            {
                StatusMessage = "The external login was not removed.";
                return RedirectToPage();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "The external login was removed.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostLinkMinecraftAsync()
        {
            if (Input.MinecraftUsername == null)
                return await OnGetAsync();

            ApplicationUser user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Unable to load user with ID 'user.Id'.");
            }

            string minecraftId = await _minecraftService.GetUuidFromUsername(Input.MinecraftUsername);

            if (minecraftId == null)
            {
                StatusMessage = "Error Minecraft username was invalid";
                return await OnGetAsync();
            }

            await _userManager.RemoveClaimsAsync(user,
                (await _userManager.GetClaimsAsync(user)).Where(x => x.Type == "urn:minecraft:user:id"));
            await _userManager.AddClaimAsync(user,
                new Claim("urn:minecraft:user:id", new Guid(minecraftId).ToString()));
            await _signInManager.RefreshSignInAsync(user);

            List<EmbedField> fields = new()
            {
                new EmbedField("User Name", user.UserName!, true),
                new EmbedField("Minecraft User name", Input.MinecraftUsername, true),
                new EmbedField("UUID", minecraftId, true)
            };

            await _loggingQueue.QueueBackgroundWorkItemAsync(new Embed
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
            string redirectUrl = Url.Page("./ExternalLogins", "LinkLoginCallback");
            AuthenticationProperties properties =
                _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl,
                    _userManager.GetUserId(User));
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetLinkLoginCallbackAsync()
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID 'user.Id'.");
            }

            ExternalLoginInfo info = await _signInManager.GetExternalLoginInfoAsync(user.Id);
            if (info == null)
            {
                throw new InvalidOperationException(
                    $"Unexpected error occurred loading external login info for user with ID '{user.Id}'.");
            }

            IdentityResult result = await _userManager.AddLoginAsync(user, info);
            if (!result.Succeeded)
            {
                StatusMessage =
                    "The external login was not added. External logins can only be associated with one account.";
                return RedirectToPage();
            }

            List<EmbedField> fields = new()
            {
                new EmbedField("User Name", user.UserName!, true),
                new EmbedField("Provider", info.LoginProvider, true)
            };

            await _loggingQueue.QueueBackgroundWorkItemAsync(new Embed
            {
                Title = "User linked new OAuth provider",
                Description = "A user has linked a new OAuth provider to their account!",
                Colour = Color.Green,
                Fields = fields
            });


            await _eventsQueue.QueueBackgroundWorkItemAsync(async (events, scope, _) =>
            {
                await events._refreshContentEvent.InvokeAsync(scope, new List<string> {info.LoginProvider});
                await events._postRefreshContentEvent.InvokeAsync(scope);
            });

            if (info.LoginProvider == "Minecraft")
            {
                await _userManager.RemoveClaimsAsync(user,
                    (await _userManager.GetClaimsAsync(user)).Where(x => x.Type == "urn:minecraft:user:id"));
            }

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            await _signInManager.RefreshSignInAsync(user);

            StatusMessage = "The external login was added.";
            return RedirectToPage();
        }
    }
}