using bcnofficechallengeapi.Data;
using bcnofficechallengeapi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bcnofficechallengeapi.Controllers;

[ApiController]
[Route("[controller]")]
public class SponsorsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SponsorResponse>>> GetAll()
    {
        var sponsors = await db.Sponsors
            .OrderBy(s => s.Name)
            .ToListAsync();
        var curiosities = await db.Curiosities
            .ToDictionaryAsync(curiosity => curiosity.SponsorId, curiosity => curiosity.Text);

        return Ok(sponsors.Select(s => ToResponse(s, curiosities.GetValueOrDefault(s.Id))));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SponsorResponse>> GetById(Guid id)
    {
        var sponsor = await db.Sponsors.FindAsync(id);

        if (sponsor is null)
        {
            return NotFound(new { error = $"Sponsor with id {id} was not found." });
        }

        var curiosity = await db.Curiosities
            .Where(item => item.SponsorId == id)
            .Select(item => item.Text)
            .FirstOrDefaultAsync();

        return Ok(ToResponse(sponsor, curiosity));
    }

    private static SponsorResponse ToResponse(Sponsor sponsor, string? curiosity) => new()
    {
        Id = sponsor.Id,
        Name = sponsor.Name,
        Description = sponsor.Description,
        Url = sponsor.Url,
        ImageUrl = sponsor.ImageUrl,
        Curiosity = curiosity
    };
}

public class SponsorResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Url { get; set; }
    public string? ImageUrl { get; set; }
    public string? Curiosity { get; set; }
}
