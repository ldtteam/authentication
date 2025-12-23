using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using LDTTeam.Authentication.Messages.User;
using LDTTeam.Authentication.Modules.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Wolverine;

namespace LDTTeam.Authentication.Server.Pages.Account.Manage
{
    [Authorize]
    public class ManageIndexModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IMessageBus bus)
        : PageModel
    {
        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Display(Name = "Username")]
            public required string Username { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            string? userName = await userManager.GetUserNameAsync(user);

            Input = new InputModel
            {
                Username = userName ?? "Unknown"
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            ApplicationUser? user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ApplicationUser? user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            string? username = await userManager.GetUserNameAsync(user);
            if (Input.Username != username)
            {
                IdentityResult setUsernameResult = await userManager.SetUserNameAsync(user, Input.Username);
                if (!setUsernameResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set username.";
                    return RedirectToPage();
                }
            }

            await signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";

            await bus.PublishAsync(new NewUserCreatedOrUpdated(
                Guid.Parse(user.Id), Input.Username
            ));
            
            return RedirectToPage();
        }
    }
}
