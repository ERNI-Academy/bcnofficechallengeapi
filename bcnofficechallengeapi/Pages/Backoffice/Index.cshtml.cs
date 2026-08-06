using bcnofficechallengeapi.Data;
using bcnofficechallengeapi.Models;
using bcnofficechallengeapi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace bcnofficechallengeapi.Pages.Backoffice;

[Authorize(AuthenticationSchemes = "BackofficeCookie")]
public class IndexModel(AppDbContext db, QrTokenService qrTokens) : PageModel
{
    private readonly Dictionary<Guid, string> qrJsonByRoom = [];
    public List<Sponsor> Sponsors { get; set; } = [];

    [BindProperty]
    public Sponsor SponsorForm { get; set; } = new();

    public async Task OnGetAsync()
    {
        Sponsors = await db.Sponsors.OrderBy(s => s.Name).ToListAsync();
    }

    public string GetQrJson(Sponsor sponsor)
    {
        if (qrJsonByRoom.TryGetValue(sponsor.Id, out var existing))
            return existing;

        var created = JsonSerializer.Serialize(new
        {
            v = 1,
            token = qrTokens.Protect(sponsor.QrId)
        });
        qrJsonByRoom[sponsor.Id] = created;
        return created;
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (SponsorForm.Id == Guid.Empty)
        {
            SponsorForm.QrId = Guid.NewGuid();
            db.Sponsors.Add(SponsorForm);
        }
        else
        {
            var existing = await db.Sponsors.FindAsync(SponsorForm.Id);
            if (existing is not null)
            {
                existing.Name = SponsorForm.Name;
                existing.Description = SponsorForm.Description;
                existing.Url = SponsorForm.Url;
                existing.ImageUrl = SponsorForm.ImageUrl;
            }
        }

        await db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var sponsor = await db.Sponsors.FindAsync(id);
        if (sponsor is not null)
        {
            db.Sponsors.Remove(sponsor);
            await db.SaveChangesAsync();
        }

        return RedirectToPage();
    }
}
