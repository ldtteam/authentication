using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Api.Events;
using LDTTeam.Authentication.Modules.Api.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace LDTTeam.Authentication.Server.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ExternalLoginModel> _logger;
        private readonly IBackgroundEventsQueue _eventsQueue;

        public ExternalLoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<ExternalLoginModel> logger,
            IBackgroundEventsQueue eventsQueue)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _eventsQueue = eventsQueue;
        }

        public string ProviderDisplayName { get; set; } = null!;

        public string? ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; } = null!;

        public IActionResult OnGetAsync()
        {
            return RedirectToPage("./Login");
        }

        public IActionResult OnPost(string provider, string? returnUrl = null)
        {
            // Request a redirect to the external login provider.
            string redirectUrl = Url.Page("./ExternalLogin", "Callback", new {returnUrl});
            AuthenticationProperties properties =
                _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
        {
            returnUrl ??= Url.Content("~/");
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToPage("./Login", new {ReturnUrl = returnUrl});
            }

            ExternalLoginInfo info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new {ReturnUrl = returnUrl});
            }

            AuthenticationProperties props = new();
            props.StoreTokens(info.AuthenticationTokens);
            props.IsPersistent = true;

            // Sign in the user with this external login provider if the user already has a login.
            SignInResult result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey,
                false, true);
            if (result.Succeeded)
            {
                ApplicationUser user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                await _signInManager.SignInWithClaimsAsync(user, info.AuthenticationProperties,
                    info.Principal.Claims.Select(c => new Claim(c.Type, c.Value)));

                _logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity?.Name,
                    info.LoginProvider);
                return LocalRedirect(returnUrl);
            }

            if (result.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }

            return await OnPostConfirmationAsync(returnUrl);
        }

        public async Task<IActionResult> OnPostConfirmationAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            // Get the information about the user from the external login provider
            ExternalLoginInfo info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information during confirmation.";
                return RedirectToPage("./Login", new {ReturnUrl = returnUrl});
            }

            string? name = info.Principal.Identity?.Name;

            name = name?.Replace(" ", "");

            ApplicationUser user = new() {UserName = name};

            IdentityResult result = await _userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                result = await _userManager.AddLoginAsync(user, info);
                if (result.Succeeded)
                {
                    await _eventsQueue.QueueBackgroundWorkItemAsync((events, scope, _) =>
                        events._refreshContentEvent.InvokeAsync(scope, new List<string> {info.LoginProvider}));

                    _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

                    AuthenticationProperties props = new();
                    props.StoreTokens(info.AuthenticationTokens);
                    props.IsPersistent = true;

                    await _signInManager.SignInAsync(user, props, info.LoginProvider);

                    return LocalRedirect(returnUrl);
                }
            }
            else if (result.Errors.Any(x => x.Code == "DuplicateUserName"))
            {
                user = new ApplicationUser {UserName = $"{info.Principal.Identity?.Name}{Guid.NewGuid():N}"};
                result = await _userManager.CreateAsync(user);

                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        await _eventsQueue.QueueBackgroundWorkItemAsync((events, scope, _) =>
                            events._refreshContentEvent.InvokeAsync(scope, new List<string> {info.LoginProvider}));

                        _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

                        AuthenticationProperties props = new();
                        props.StoreTokens(info.AuthenticationTokens);
                        props.IsPersistent = true;

                        await _signInManager.SignInAsync(user, props, info.LoginProvider);

                        return LocalRedirect(returnUrl);
                    }
                }
            }

            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ProviderDisplayName = info.ProviderDisplayName;
            ReturnUrl = returnUrl;
            return Page();
        }
    }
}