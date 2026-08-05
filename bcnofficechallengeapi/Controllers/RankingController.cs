using bcnofficechallengeapi.Data;
using bcnofficechallengeapi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bcnofficechallengeapi.Controllers;

[ApiController]
[Route("[controller]")]
public class RankingController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RankingEntryResponse>>> GetRanking()
    {
        var ranking = await db.Ranking
            .OrderByDescending(r => r.Points)
            .ThenByDescending(r => r.PointsTimestamp.HasValue)
            .ThenBy(r => r.PointsTimestamp)
            .Select(r => new RankingEntryResponse
            {
                Id = r.UserId,
                Name = r.Name,
                Email = r.Email,
                Points = r.Points,
                PointsTimestamp = r.PointsTimestamp
            })
            .ToListAsync();

        return Ok(ranking);
    }
}

public class RankingEntryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Points { get; set; }
    public DateTime? PointsTimestamp { get; set; }
}
