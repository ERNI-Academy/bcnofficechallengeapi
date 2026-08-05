namespace bcnofficechallengeapi.Models;

public class User
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? CompanyName { get; set; }

    public string? JobTitle { get; set; }

    public int Points { get; set; }

    public DateTime? PointsTimestamp { get; set; }

    public string? LinkedIn { get; set; }

    public string Password { get; set; } = string.Empty;
}
