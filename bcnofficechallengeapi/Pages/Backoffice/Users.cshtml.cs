using bcnofficechallengeapi.Data;
using bcnofficechallengeapi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace bcnofficechallengeapi.Pages.Backoffice;

[Authorize(AuthenticationSchemes = "BackofficeCookie")]
public class UsersModel(AppDbContext db) : PageModel
{
    public List<User> Users { get; set; } = [];
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public string? FilterName { get; set; }
    public string? FilterEmail { get; set; }

    [BindProperty]
    public UserForm Form { get; set; } = new();

    private const int PageSize = 25;

    public async Task OnGetAsync(string? name, string? email, int page = 1)
    {
        FilterName = name;
        FilterEmail = email;
        CurrentPage = Math.Max(1, page);

        var query = db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(u => u.Name.Contains(name));

        if (!string.IsNullOrWhiteSpace(email))
            query = query.Where(u => u.Email.Contains(email));

        TotalCount = await query.CountAsync();
        TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

        Users = await query
            .OrderBy(u => u.Name)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        Form.Email = Form.Email.Trim().ToLowerInvariant();
        if (!IsAllowedEmail(Form.Email))
        {
            TempData["Error"] = "Email must use the @betterask.erni domain.";
            return RedirectToPage();
        }

        if (await db.Users.AnyAsync(user => user.Email == Form.Email && user.Id != Form.Id))
        {
            TempData["Error"] = "Email is already registered.";
            return RedirectToPage();
        }

        if (Form.Id == Guid.Empty)
        {
            if (string.IsNullOrWhiteSpace(Form.Password))
            {
                TempData["Error"] = "Password is required for new users.";
                return RedirectToPage();
            }

            db.Users.Add(new User
            {
                Name = Form.Name,
                Email = Form.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(Form.Password),
                CompanyName = Form.CompanyName,
                JobTitle = Form.JobTitle,
                Points = 0,
            });
        }
        else
        {
            var existing = await db.Users.FindAsync(Form.Id);
            if (existing is not null)
            {
                existing.Name = Form.Name;
                existing.Email = Form.Email;
                existing.CompanyName = Form.CompanyName;
                existing.JobTitle = Form.JobTitle;
                if (!string.IsNullOrWhiteSpace(Form.Password))
                    existing.Password = BCrypt.Net.BCrypt.HashPassword(Form.Password);
            }
        }

        await db.SaveChangesAsync();
        return RedirectToPage();
    }

    private static bool IsAllowedEmail(string email) =>
        email.EndsWith("@betterask.erni", StringComparison.OrdinalIgnoreCase) &&
        email.Length > "@betterask.erni".Length;

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is not null)
        {
            db.Users.Remove(user);
            await db.SaveChangesAsync();
        }

        return RedirectToPage();
    }
}

public class UserForm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? CompanyName { get; set; }
    public string? JobTitle { get; set; }
}
