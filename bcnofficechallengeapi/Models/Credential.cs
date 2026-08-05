namespace bcnofficechallengeapi.Models;

public class Credential
{
    public int Id { get; set; }

    public string AppUser { get; set; } = string.Empty;

    public string AppPass { get; set; } = string.Empty;
}
