using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduBoost.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IWebHostEnvironment environment,
            ILogger<IndexModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _environment = environment;
            _logger = logger;
        }

        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "Teacher";
        public string? ProfilePicture { get; set; }
        public string DisplayInitials { get; set; } = "U";

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusType { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty]
        public PasswordInputModel PasswordInput { get; set; } = new();

        public class InputModel
        {
            [Display(Name = "Full Name")]
            [StringLength(80)]
            public string? FullName { get; set; }

            [Phone]
            [Display(Name = "Phone number")]
            public string? PhoneNumber { get; set; }

            [Display(Name = "Role / Department")]
            [StringLength(100)]
            public string? Department { get; set; }

            [Display(Name = "Bio / Notes")]
            [StringLength(300)]
            public string? Bio { get; set; }

            public IFormFile? AvatarFile { get; set; }

            [Display(Name = "Google Drive or Image URL")]
            public string? AvatarUrl { get; set; }

            public bool RemoveAvatar { get; set; }
        }

        public class PasswordInputModel
        {
            [DataType(DataType.Password)]
            [Display(Name = "Current password")]
            public string? CurrentPassword { get; set; }

            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New password")]
            public string? NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm new password")]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
            public string? ConfirmPassword { get; set; }
        }

        private async Task LoadUserDataAsync(IdentityUser user)
        {
            Username = user.UserName ?? user.Email ?? "User";
            Email = user.Email ?? "";
            var roles = await _userManager.GetRolesAsync(user);
            Role = roles.FirstOrDefault() ?? "Teacher";

            var claims = await _userManager.GetClaimsAsync(user);
            ProfilePicture = claims.FirstOrDefault(c => c.Type == "ProfilePicture")?.Value;
            var fullName = claims.FirstOrDefault(c => c.Type == "FullName")?.Value;
            var department = claims.FirstOrDefault(c => c.Type == "Department")?.Value;
            var bio = claims.FirstOrDefault(c => c.Type == "Bio")?.Value;

            Input.FullName = fullName ?? (Username.Contains('@') ? Username.Split('@')[0] : Username);
            Input.PhoneNumber = user.PhoneNumber;
            Input.Department = department ?? (Role == "Admin" ? "Academic Administration" : "Remedial Teaching Department");
            Input.Bio = bio ?? "";

            // Compute initials
            var nameToUse = !string.IsNullOrWhiteSpace(Input.FullName) ? Input.FullName : Username;
            var parts = nameToUse.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                DisplayInitials = (parts[0][0].ToString() + parts[1][0].ToString()).ToUpperInvariant();
            }
            else if (nameToUse.Length > 0)
            {
                DisplayInitials = nameToUse.Substring(0, 1).ToUpperInvariant();
            }
        }

        private async Task SetUserClaimAsync(IdentityUser user, string claimType, string? claimValue)
        {
            var claims = await _userManager.GetClaimsAsync(user);
            var existingClaim = claims.FirstOrDefault(c => c.Type == claimType);

            if (existingClaim != null)
            {
                if (!string.IsNullOrWhiteSpace(claimValue))
                {
                    await _userManager.ReplaceClaimAsync(user, existingClaim, new Claim(claimType, claimValue));
                }
                else
                {
                    await _userManager.RemoveClaimAsync(user, existingClaim);
                }
            }
            else if (!string.IsNullOrWhiteSpace(claimValue))
            {
                await _userManager.AddClaimAsync(user, new Claim(claimType, claimValue));
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadUserDataAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Handle Phone Number update
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    StatusType = "danger";
                    await LoadUserDataAsync(user);
                    return Page();
                }
            }

            // Handle Avatar removal or update
            if (Input.RemoveAvatar)
            {
                await SetUserClaimAsync(user, "ProfilePicture", null);
            }
            else if (Input.AvatarFile != null && Input.AvatarFile.Length > 0)
            {
                var ext = Path.GetExtension(Input.AvatarFile.FileName).ToLowerInvariant();
                var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                if (allowedExts.Contains(ext))
                {
                    var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
                    Directory.CreateDirectory(uploadsDir);

                    var uniqueFileName = $"avatar_{user.Id}_{Guid.NewGuid():N}{ext}";
                    var filePath = Path.Combine(uploadsDir, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await Input.AvatarFile.CopyToAsync(stream);
                    }

                    var relativeUrl = $"/uploads/avatars/{uniqueFileName}";
                    await SetUserClaimAsync(user, "ProfilePicture", relativeUrl);
                }
                else
                {
                    StatusMessage = "Invalid image file type. Please upload a JPG, PNG, WEBP, or GIF image.";
                    StatusType = "danger";
                    await LoadUserDataAsync(user);
                    return Page();
                }
            }
            else if (!string.IsNullOrWhiteSpace(Input.AvatarUrl))
            {
                // Format Google Drive link if applicable
                var cleanUrl = Input.AvatarUrl.Trim();
                if (cleanUrl.Contains("drive.google.com/file/d/"))
                {
                    var fileId = cleanUrl.Split("/file/d/")[1].Split("/")[0].Split("?")[0];
                    cleanUrl = $"https://lh3.googleusercontent.com/d/{fileId}";
                }
                await SetUserClaimAsync(user, "ProfilePicture", cleanUrl);
            }

            // Save custom details
            await SetUserClaimAsync(user, "FullName", Input.FullName);
            await SetUserClaimAsync(user, "Department", Input.Department);
            await SetUserClaimAsync(user, "Bio", Input.Bio);

            // Refresh authentication cookie with new claims immediately
            await _signInManager.RefreshSignInAsync(user);

            StatusMessage = "Profile updated successfully! Your changes and new photo are now live across the portal.";
            StatusType = "success";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (string.IsNullOrEmpty(PasswordInput.CurrentPassword) || string.IsNullOrEmpty(PasswordInput.NewPassword))
            {
                StatusMessage = "Please enter both your current password and a new password.";
                StatusType = "danger";
                await LoadUserDataAsync(user);
                return Page();
            }

            if (PasswordInput.NewPassword != PasswordInput.ConfirmPassword)
            {
                StatusMessage = "The new password and confirmation password do not match.";
                StatusType = "danger";
                await LoadUserDataAsync(user);
                return Page();
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, PasswordInput.CurrentPassword, PasswordInput.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                var errors = string.Join(" ", changePasswordResult.Errors.Select(e => e.Description));
                StatusMessage = $"Password change failed: {errors}";
                StatusType = "danger";
                await LoadUserDataAsync(user);
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your password has been changed successfully.";
            StatusType = "success";
            return RedirectToPage();
        }
    }
}
