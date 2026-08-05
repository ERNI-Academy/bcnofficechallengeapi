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
            .Select(s => ToResponse(s))
            .ToListAsync();

        return Ok(sponsors);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SponsorResponse>> GetById(Guid id)
    {
        var sponsor = await db.Sponsors.FindAsync(id);

        if (sponsor is null)
        {
            return NotFound(new { error = $"Sponsor with id {id} was not found." });
        }

        return Ok(ToResponse(sponsor));
    }

    [HttpGet("by-qr/{qrId}")]
    public async Task<ActionResult<SponsorResponse>> GetByQrId(Guid qrId)
    {
        var sponsor = await db.Sponsors.FirstOrDefaultAsync(s => s.QrId == qrId);

        if (sponsor is null)
        {
            return NotFound(new { error = "Invalid QR code." });
        }

        return Ok(ToResponse(sponsor));
    }

    private static SponsorResponse ToResponse(Sponsor sponsor) => new()
    {
        Id = sponsor.Id,
        Name = sponsor.Name,
        Description = sponsor.Description,
        Url = sponsor.Url,
        ImageUrl = sponsor.ImageUrl,
        PointsValue = sponsor.PointsValue
    };
}

public class SponsorResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Url { get; set; }
    public string? ImageUrl { get; set; }
    public int PointsValue { get; set; }
}
