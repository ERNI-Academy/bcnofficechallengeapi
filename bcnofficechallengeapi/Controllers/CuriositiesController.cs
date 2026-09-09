using System.Security.Claims;
using bcnofficechallengeapi.Data;
using bcnofficechallengeapi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bcnofficechallengeapi.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(AuthenticationSchemes = "ParticipantJwt")]
public class CuriositiesController(AppDbContext db) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<IEnumerable<Guid>>> GetMine()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var sponsorIds = await db.UserCuriosityViews
            .AsNoTracking()
            .Where(view => view.UserId == userId)
            .Select(view => view.SponsorId)
            .ToListAsync();

        return Ok(sponsorIds);
    }

    [HttpPost("{sponsorId:guid}/view")]
    public async Task<IActionResult> MarkViewed(Guid sponsorId)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var hasCuriosity = await db.Curiosities.AnyAsync(curiosity => curiosity.SponsorId == sponsorId);
        if (!hasCuriosity)
            return NotFound(new { error = "This room does not have a curiosity." });

        if (await db.UserCuriosityViews.AnyAsync(view =>
                view.UserId == userId && view.SponsorId == sponsorId))
        {
            return NoContent();
        }

        db.UserCuriosityViews.Add(new UserCuriosityView
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SponsorId = sponsorId,
            ViewedAt = DateTime.UtcNow
        });

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return NoContent();
        }

        return NoContent();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var subject = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(subject, out userId);
    }
}
