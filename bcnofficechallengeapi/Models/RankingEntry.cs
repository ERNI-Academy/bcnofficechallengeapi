namespace bcnofficechallengeapi.Models;

public class RankingEntry
{
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Points { get; set; }

    public string Email { get; set; } = string.Empty;

    public DateTime? PointsTimestamp { get; set; }
}
