using bcnofficechallengeapi.Data;
using bcnofficechallengeapi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace bcnofficechallengeapi.Pages.Backoffice;

[Authorize(AuthenticationSchemes = "BackofficeCookie")]
public class CuriositiesModel(AppDbContext db) : PageModel
{
    public List<Sponsor> Rooms { get; set; } = [];
    public List<Curiosity> Curiosities { get; set; } = [];

    public IEnumerable<Sponsor> AvailableRooms =>
        Rooms.Where(room => Curiosities.All(curiosity => curiosity.SponsorId != room.Id));

    [BindProperty]
    public CuriosityForm Form { get; set; } = new();

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAddAsync()
    {
        var text = Form.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            TempData["Error"] = "Curiosity text is required.";
            return RedirectToPage();
        }

        if (!await db.Sponsors.AnyAsync(room => room.Id == Form.SponsorId))
        {
            TempData["Error"] = "Select a valid room.";
            return RedirectToPage();
        }

        if (await db.Curiosities.AnyAsync(curiosity => curiosity.SponsorId == Form.SponsorId))
        {
            TempData["Error"] = "This room already has a curiosity.";
            return RedirectToPage();
        }

        db.Curiosities.Add(new Curiosity
        {
            Id = Guid.NewGuid(),
            SponsorId = Form.SponsorId,
            Text = text
        });
        await db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        var curiosity = await db.Curiosities.FindAsync(Form.Id);
        if (curiosity is null)
            return RedirectToPage();

        var text = Form.Text.Trim();
        if (string.IsNullOrWhiteSpace(text) ||
            !await db.Sponsors.AnyAsync(room => room.Id == Form.SponsorId))
        {
            TempData["Error"] = "Curiosity text and room are required.";
            return RedirectToPage();
        }

        if (await db.Curiosities.AnyAsync(item =>
                item.Id != curiosity.Id && item.SponsorId == Form.SponsorId))
        {
            TempData["Error"] = "This room already has a curiosity.";
            return RedirectToPage();
        }

        curiosity.SponsorId = Form.SponsorId;
        curiosity.Text = text;
        await db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var curiosity = await db.Curiosities.FindAsync(id);
        if (curiosity is not null)
        {
            db.Curiosities.Remove(curiosity);
            await db.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Rooms = await db.Sponsors.OrderBy(room => room.Name).ToListAsync();
        Curiosities = await db.Curiosities
            .OrderBy(curiosity => curiosity.SponsorId)
            .ToListAsync();
    }
}

public class CuriosityForm
{
    public Guid Id { get; set; }
    public Guid SponsorId { get; set; }
    public string Text { get; set; } = string.Empty;
}
