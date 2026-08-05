using System.Security.Claims;
using bcnofficechallengeapi.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace bcnofficechallengeapi.Pages.Backoffice;

public class LoginModel(AppDbContext db) : PageModel
{
    [BindProperty]
    public string User { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var credential = await db.Credentials
            .FirstOrDefaultAsync(c => c.AppUser == User && c.AppPass == Password);

        if (credential is null)
        {
            ErrorMessage = "Invalid user or password.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, User),
            new("BackofficeAccess", "true")
        };

        var identity = new ClaimsIdentity(claims, "BackofficeCookie");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("BackofficeCookie", principal);

        return RedirectToPage("/Backoffice/Index");
    }

    public async Task<IActionResult> OnGetLogoutAsync()
    {
        await HttpContext.SignOutAsync("BackofficeCookie");
        return RedirectToPage("/Backoffice/Login");
    }
}
