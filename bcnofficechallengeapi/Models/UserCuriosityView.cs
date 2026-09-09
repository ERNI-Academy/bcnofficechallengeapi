namespace bcnofficechallengeapi.Models;

public class UserCuriosityView
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid SponsorId { get; set; }

    public DateTime ViewedAt { get; set; }
}
