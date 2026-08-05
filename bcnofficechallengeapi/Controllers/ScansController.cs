using bcnofficechallengeapi.Data;
using bcnofficechallengeapi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bcnofficechallengeapi.Controllers;

[ApiController]
[Route("[controller]")]
public class ScansController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> RegisterScan(RegisterScanRequest request)
    {
        var deadline = new DateTime(2026, 4, 21, 13, 0, 0, DateTimeKind.Utc);
        if (DateTime.UtcNow > deadline)
            return BadRequest(new { error = "The time to participate has ended." });

        var user = await db.Users.FindAsync(request.UserId);
        if (user is null)
            return NotFound(new { error = $"User with id {request.UserId} was not found." });

        var sponsor = await db.Sponsors.FirstOrDefaultAsync(s => s.QrId == request.QrId);
        if (sponsor is null)
            return NotFound(new { error = "Invalid QR code." });

        var alreadyScanned = await db.UserSponsorScans
            .AnyAsync(s => s.UserId == request.UserId && s.SponsorId == sponsor.Id);

        if (alreadyScanned)
            return Conflict(new { error = "This sponsor has already been scanned by this user." });

        var scan = new UserSponsorScan
        {
            UserId = request.UserId,
            SponsorId = sponsor.Id,
            ScannedAt = DateTime.UtcNow
        };

        user.Points += sponsor.PointsValue;
        user.PointsTimestamp = DateTime.UtcNow;

        db.UserSponsorScans.Add(scan);
        await db.SaveChangesAsync();

        return Created(string.Empty, new { scan.UserId, scan.SponsorId, scan.ScannedAt, user.Points });
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<IEnumerable<ScannedSponsorResponse>>> GetByUser(Guid userId)
    {
        var userExists = await db.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
            return NotFound(new { error = $"User with id {userId} was not found." });

        var scans = await db.UserSponsorScans
            .Where(s => s.UserId == userId)
            .Select(s => new ScannedSponsorResponse
            {
                SponsorId = s.SponsorId,
                ScannedAt = s.ScannedAt
            })
            .ToListAsync();

        return Ok(scans);
    }
}

public class RegisterScanRequest
{
    public Guid UserId { get; set; }
    public Guid QrId { get; set; }
}

public class ScannedSponsorResponse
{
    public Guid SponsorId { get; set; }
    public DateTime ScannedAt { get; set; }
}
