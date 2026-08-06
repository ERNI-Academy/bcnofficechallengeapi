namespace bcnofficechallengeapi.Models;

public class UserSponsorScan
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid SponsorId { get; set; }

    public DateTime ScannedAt { get; set; }

    public int PointsAwarded { get; set; }

    public int MaximumPoints { get; set; }
}
