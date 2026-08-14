namespace bcnofficechallengeapi.Models;

public class UserSponsorScanAnswer
{
    public Guid Id { get; set; }

    public Guid UserSponsorScanId { get; set; }

    public Guid QuestionId { get; set; }

    public string QuestionText { get; set; } = string.Empty;

    public bool SelectedAnswer { get; set; }

    public bool CorrectAnswer { get; set; }

    public bool IsCorrect { get; set; }

    public int PointsAwarded { get; set; }
}
