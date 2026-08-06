namespace bcnofficechallengeapi.Models;

public class Question
{
    public Guid Id { get; set; }

    public Guid SponsorId { get; set; }

    public string Text { get; set; } = string.Empty;

    public bool CorrectAnswer { get; set; }

    public int Points { get; set; }

    public int SortOrder { get; set; }
}
