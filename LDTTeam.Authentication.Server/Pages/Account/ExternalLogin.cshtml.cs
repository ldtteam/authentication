using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Messages.User;
using LDTTeam.Authentication.Models.App.User;
using LDTTeam.Authentication.Modules.Api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Wolverine;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace LDTTeam.Authentication.Server.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<ExternalLoginModel> logger,
        IMessageBus bus)
        : PageModel
    {
        public string? ProviderDisplayName { get; set; }

        public string? ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; } = null!;

        public IActionResult OnGetAsync()
        {
            return RedirectToPage("./Login");
        }

        public IActionResult OnPost(string provider, string? returnUrl)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Page("./ExternalLogin", "Callback", new {returnUrl});
            AuthenticationProperties properties =
                signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl, string? remoteError, CancellationToken token)
        {
            returnUrl ??= Url.Content("~/");
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToPage("./Login", new {ReturnUrl = returnUrl});
            }

            ExternalLoginInfo? info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new {ReturnUrl = returnUrl});
            }

            AuthenticationProperties props = new();
            props.StoreTokens(info.AuthenticationTokens ?? []);
            props.IsPersistent = true;

            // Sign in the user with this external login provider if the user already has a login.
            SignInResult result = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey,
                false, true);
            if (result.Succeeded)
            {
                ApplicationUser? user = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (user == null)
                {
                    ErrorMessage = "Error loading external login information.";
                    return RedirectToPage("./Login", new {ReturnUrl = returnUrl});
                }
                await signInManager.SignInWithClaimsAsync(user, info.AuthenticationProperties,
                    info.Principal.Claims.Select(c => new Claim(c.Type, c.Value)));

                logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity?.Name,
                    info.LoginProvider);
                return LocalRedirect(returnUrl);
            }

            if (result.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }

            return await OnPostConfirmationAsync(returnUrl, token);
        }

        public async Task<IActionResult> OnPostConfirmationAsync(string? returnUrl, CancellationToken token)
        {
            returnUrl ??= Url.Content("~/");
            // Get the information about the user from the external login provider
            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information during confirmation.";
                return RedirectToPage("./Login", new {ReturnUrl = returnUrl});
            }

            string? name = info.Principal.Identity?.Name;

            name = name?.Replace(" ", "");

            ApplicationUser user = new() {UserName = name};

            IdentityResult result = await userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                var userId = Guid.Parse(user.Id);
                await bus.PublishAsync(
                    new NewUserCreatedOrUpdated(userId, name!)
                );
                
                result = await userManager.AddLoginAsync(user, info);
                if (result.Succeeded)
                {
                    logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
                    await bus.PublishAsync(
                        new ExternalLoginConnectedToUser(userId, Enum.Parse<AccountProvider>(info.LoginProvider),
                            info.ProviderKey));

                    AuthenticationProperties props = new();
                    props.StoreTokens(info.AuthenticationTokens ?? []);
                    props.IsPersistent = true;

                    await signInManager.SignInAsync(user, props, info.LoginProvider);

                    return LocalRedirect(returnUrl);
                }
            }
            else if (result.Errors.Any(x => x.Code == "DuplicateUserName"))
            {
                name = $"{name}{Guid.NewGuid():N}";
                user = new ApplicationUser {UserName = name};
                result = await userManager.CreateAsync(user);

                if (result.Succeeded)
                {
                    var userId = Guid.Parse(user.Id);
                    await bus.PublishAsync(
                        new NewUserCreatedOrUpdated(userId, name!)
                    );
                    
                    result = await userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        await bus.PublishAsync(
                            new ExternalLoginConnectedToUser(userId, Enum.Parse<AccountProvider>(info.LoginProvider),
                                info.ProviderKey));

                        logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

                        AuthenticationProperties props = new();
                        props.StoreTokens(info.AuthenticationTokens ?? []);
                        props.IsPersistent = true;

                        await signInManager.SignInAsync(user, props, info.LoginProvider);

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
