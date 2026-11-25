using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduBoost.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email address is required")]
            [EmailAddress(ErrorMessage = "Please enter a valid email address")]
            public string Email { get; set; } = "";

            [Required(ErrorMessage = "Password is required")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = "";

            [Display(Name = "Remember me")]
            public bool RememberMe { get; set; } = true;
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : Redirect("~/");
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid email or password. Please try again.");
                    return Page();
                }
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
                _logger.LogInformation("User logged in via {Provider}.", provider);
                return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : Redirect("~/");
            }

            ModelState.AddModelError(string.Empty, $"Unable to authenticate via {provider}.");
            return Page();
        }
    }
}
