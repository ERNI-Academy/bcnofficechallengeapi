namespace bcnofficechallengeapi.Models;

public class Curiosity
{
    public Guid Id { get; set; }

    public Guid SponsorId { get; set; }

    public string Text { get; set; } = string.Empty;
}
