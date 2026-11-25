using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduBoost.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public RegisterModel(UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = "";

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = "";

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = "";
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");

            if (!ModelState.IsValid) return Page();

            var user = new IdentityUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                // Every self-registered account is a Teacher by default.
                if (!await _userManager.IsInRoleAsync(user, "Teacher"))
                {
                    await _userManager.AddToRoleAsync(user, "Teacher");
                }

                await _signInManager.SignInAsync(user, isPersistent: false);
                return Url.IsLocalUrl(ReturnUrl) ? LocalRedirect(ReturnUrl) : Redirect("~/");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSocialLoginAsync(string provider, string? email = null, string? name = null, string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            if (string.IsNullOrWhiteSpace(provider))
            {
                provider = "Google";
            }

            var providerLower = provider.Trim().ToLowerInvariant();
            var defaultEmail = providerLower switch
            {
                "google" => "alex.google@eduboost.local",
                "apple" => "alex.apple@eduboost.local",
                "facebook" => "alex.facebook@eduboost.local",
                "x" or "twitter" => "alex.x@eduboost.local",
                _ => $"{providerLower}.user@eduboost.local"
            };

            var defaultName = providerLower switch
            {
                "google" => "Alex Morgan",
                "apple" => "Alex Morgan",
                "facebook" => "Alex Morgan",
                "x" or "twitter" => "Alex Morgan",
                _ => $"{provider} User"
            };

            var targetEmail = !string.IsNullOrWhiteSpace(email) ? email.Trim() : defaultEmail;
            var targetName = !string.IsNullOrWhiteSpace(name) ? name.Trim() : defaultName;

            var user = await _userManager.FindByEmailAsync(targetEmail);
            if (user == null)
            {
                user = new IdentityUser
                {
                    UserName = targetEmail,
                    Email = targetEmail,
                    EmailConfirmed = true
                };

                var createRes = await _userManager.CreateAsync(user, "Social@123456");
                if (!createRes.Succeeded)
                {
                    createRes = await _userManager.CreateAsync(user);
                }

                if (createRes.Succeeded)
                {
                    if (!await _userManager.IsInRoleAsync(user, "Teacher"))
                    {
                        await _userManager.AddToRoleAsync(user, "Teacher");
                    }
                }
            }

            if (user != null)
            {
                var claims = await _userManager.GetClaimsAsync(user);

                // Set/Update FullName
                var existingName = claims.FirstOrDefault(c => c.Type == "FullName");
                if (existingName == null)
                {
                    await _userManager.AddClaimAsync(user, new Claim("FullName", targetName));
                }
                else if (!string.IsNullOrWhiteSpace(name) && existingName.Value != name)
                {
                    await _userManager.ReplaceClaimAsync(user, existingName, new Claim("FullName", targetName));
                }

                // Set Provider claim
                var existingProvider = claims.FirstOrDefault(c => c.Type == "Provider");
                if (existingProvider == null)
                {
                    await _userManager.AddClaimAsync(user, new Claim("Provider", provider));
                }

                // Set ProfilePicture claim if not set
                var existingPic = claims.FirstOrDefault(c => c.Type == "ProfilePicture");
                if (existingPic == null)
                {
                    var avatarUrl = $"/images/social/{providerLower}-avatar.png";
                    await _userManager.AddClaimAsync(user, new Claim("ProfilePicture", avatarUrl));
                }

                // Set Department claim if not set
                var existingDept = claims.FirstOrDefault(c => c.Type == "Department");
                if (existingDept == null)
                {
                    await _userManager.AddClaimAsync(user, new Claim("Department", $"{provider} Account"));
                }

                await _signInManager.SignInAsync(user, isPersistent: true);
                return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : Redirect("~/");
            }

            ModelState.AddModelError(string.Empty, $"Unable to authenticate via {provider}.");
            return Page();
        }
    }
}
