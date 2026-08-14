using System.Data;
using System.Security.Claims;
using bcnofficechallengeapi.Data;
using bcnofficechallengeapi.Models;
using bcnofficechallengeapi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bcnofficechallengeapi.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(AuthenticationSchemes = "ParticipantJwt")]
public class ScansController(AppDbContext db, QrTokenService qrTokens) : ControllerBase
{
    private const string AlreadyCompletedMessage = "Nice try, you already scanned this code.";

    [HttpGet("me")]
    public async Task<ActionResult<IEnumerable<CompletedRoomResponse>>> GetMine()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var scans = await db.UserSponsorScans
            .AsNoTracking()
            .Include(scan => scan.AnswerResults)
            .Where(scan => scan.UserId == userId)
            .OrderBy(scan => scan.ScannedAt)
            .ToListAsync();

        return Ok(scans.Select(scan => new CompletedRoomResponse
        {
            SponsorId = scan.SponsorId,
            CompletedAt = scan.ScannedAt,
            PointsAwarded = scan.PointsAwarded,
            MaximumPoints = scan.MaximumPoints,
            AnswerResults = scan.AnswerResults
                .OrderBy(answer => answer.Id)
                .Select(ToAnswerResult)
                .ToList()
        }));
    }

    [HttpPost("prepare")]
    public async Task<ActionResult<PreparedQuestionsResponse>> Prepare(PrepareQuestionsRequest request)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var resolved = await ResolveRoomAsync(request.RoomId, request.Qr);
        if (!resolved.Ok)
            return StatusCode(resolved.Status, new { error = resolved.Error });

        if (await HasCompletedAsync(userId, resolved.Sponsor!.Id))
            return Conflict(new { errorCode = "ROOM_ALREADY_COMPLETED", error = AlreadyCompletedMessage });

        var questions = await db.Questions
            .Where(question => question.SponsorId == resolved.Sponsor.Id)
            .OrderBy(question => question.SortOrder)
            .ThenBy(question => question.Id)
            .Select(question => new PreparedQuestionResponse
            {
                Id = question.Id,
                Text = question.Text
            })
            .ToListAsync();

        if (questions.Count == 0)
            return BadRequest(new { error = "This room does not have any questions yet." });

        return Ok(new PreparedQuestionsResponse
        {
            RoomId = resolved.Sponsor.Id,
            RoomName = resolved.Sponsor.Name,
            Questions = questions
        });
    }

    [HttpPost("complete")]
    public async Task<ActionResult<CompletedQuizResponse>> Complete(CompleteQuizRequest request)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var resolved = await ResolveRoomAsync(request.RoomId, request.Qr);
        if (!resolved.Ok)
            return StatusCode(resolved.Status, new { error = resolved.Error });

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        if (await HasCompletedAsync(userId, resolved.Sponsor!.Id))
            return Conflict(new { errorCode = "ROOM_ALREADY_COMPLETED", error = AlreadyCompletedMessage });

        var user = await db.Users.FindAsync(userId);
        if (user is null)
            return Unauthorized();

        var questions = await db.Questions
            .Where(question => question.SponsorId == resolved.Sponsor.Id)
            .ToListAsync();

        if (questions.Count == 0)
            return BadRequest(new { error = "This room does not have any questions yet." });

        var answers = request.Answers ?? [];
        if (answers.Count != questions.Count || answers.Select(answer => answer.QuestionId).Distinct().Count() != questions.Count)
            return BadRequest(new { error = "Every question must be answered exactly once." });

        var answersByQuestion = answers.ToDictionary(answer => answer.QuestionId, answer => answer.Answer);
        if (questions.Any(question => !answersByQuestion.ContainsKey(question.Id)))
            return BadRequest(new { error = "One or more answers do not belong to this room." });

        var maximumPoints = questions.Sum(question => question.Points);
        var calculatedAnswers = questions
            .Select(question =>
            {
                var selectedAnswer = answersByQuestion[question.Id];
                var isCorrect = selectedAnswer == question.CorrectAnswer;
                return new CalculatedQuizAnswer(
                    question.Id,
                    question.Text,
                    selectedAnswer,
                    question.CorrectAnswer,
                    isCorrect,
                    isCorrect ? question.Points : 0);
            })
            .ToList();
        var pointsAwarded = calculatedAnswers.Sum(answer => answer.PointsAwarded);
        var completedAt = DateTime.UtcNow;

        var scan = new UserSponsorScan
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SponsorId = resolved.Sponsor.Id,
            ScannedAt = completedAt,
            PointsAwarded = pointsAwarded,
            MaximumPoints = maximumPoints
        };

        db.UserSponsorScans.Add(scan);
        db.UserSponsorScanAnswers.AddRange(calculatedAnswers.Select(answer => new UserSponsorScanAnswer
        {
            Id = Guid.NewGuid(),
            UserSponsorScanId = scan.Id,
            QuestionId = answer.QuestionId,
            QuestionText = answer.QuestionText,
            SelectedAnswer = answer.SelectedAnswer,
            CorrectAnswer = answer.CorrectAnswer,
            IsCorrect = answer.IsCorrect,
            PointsAwarded = answer.PointsAwarded
        }));

        user.Points += pointsAwarded;
        user.PointsTimestamp = completedAt;

        try
        {
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return Conflict(new { errorCode = "ROOM_ALREADY_COMPLETED", error = AlreadyCompletedMessage });
        }

        return Ok(new CompletedQuizResponse
        {
            PointsEarned = pointsAwarded,
            MaximumPoints = maximumPoints,
            TotalPoints = user.Points,
            CompletedAt = completedAt,
            AnswerResults = calculatedAnswers.Select(answer => new QuizAnswerResultResponse
            {
                QuestionId = answer.QuestionId,
                QuestionText = answer.QuestionText,
                IsCorrect = answer.IsCorrect
            }).ToList()
        });
    }

    private async Task<ResolvedRoom> ResolveRoomAsync(Guid roomId, QrPayloadRequest? qr)
    {
        if (roomId == Guid.Empty || qr is null || qr.V != 1 || string.IsNullOrWhiteSpace(qr.Token))
            return ResolvedRoom.Failure(400, "Invalid QR payload.");

        if (!qrTokens.TryUnprotect(qr.Token, out var qrId))
            return ResolvedRoom.Failure(400, "Invalid or modified QR code.");

        var sponsor = await db.Sponsors.FirstOrDefaultAsync(item => item.QrId == qrId);
        if (sponsor is null)
            return ResolvedRoom.Failure(404, "Invalid QR code.");
        if (sponsor.Id != roomId)
            return ResolvedRoom.Failure(400, "This QR code belongs to a different room.");

        return ResolvedRoom.Success(sponsor);
    }

    private Task<bool> HasCompletedAsync(Guid userId, Guid sponsorId) =>
        db.UserSponsorScans.AnyAsync(scan => scan.UserId == userId && scan.SponsorId == sponsorId);

    private bool TryGetUserId(out Guid userId)
    {
        var subject = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(subject, out userId);
    }

    private static QuizAnswerResultResponse ToAnswerResult(UserSponsorScanAnswer answer) => new()
    {
        QuestionId = answer.QuestionId,
        QuestionText = answer.QuestionText,
        IsCorrect = answer.IsCorrect
    };

    private sealed record CalculatedQuizAnswer(
        Guid QuestionId,
        string QuestionText,
        bool SelectedAnswer,
        bool CorrectAnswer,
        bool IsCorrect,
        int PointsAwarded);

    private sealed record ResolvedRoom(bool Ok, int Status, string? Error, Sponsor? Sponsor)
    {
        public static ResolvedRoom Success(Sponsor sponsor) => new(true, 200, null, sponsor);
        public static ResolvedRoom Failure(int status, string error) => new(false, status, error, null);
    }
}

public class QrPayloadRequest
{
    public int V { get; set; }
    public string Token { get; set; } = string.Empty;
}

public class PrepareQuestionsRequest
{
    public Guid RoomId { get; set; }
    public QrPayloadRequest? Qr { get; set; }
}

public class CompleteQuizRequest : PrepareQuestionsRequest
{
    public List<QuizAnswerRequest>? Answers { get; set; }
}

public class QuizAnswerRequest
{
    public Guid QuestionId { get; set; }
    public bool Answer { get; set; }
}

public class PreparedQuestionsResponse
{
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public List<PreparedQuestionResponse> Questions { get; set; } = [];
}

public class PreparedQuestionResponse
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class CompletedQuizResponse
{
    public int PointsEarned { get; set; }
    public int MaximumPoints { get; set; }
    public int TotalPoints { get; set; }
    public DateTime CompletedAt { get; set; }
    public List<QuizAnswerResultResponse> AnswerResults { get; set; } = [];
}

public class CompletedRoomResponse
{
    public Guid SponsorId { get; set; }
    public DateTime CompletedAt { get; set; }
    public int PointsAwarded { get; set; }
    public int MaximumPoints { get; set; }
    public List<QuizAnswerResultResponse> AnswerResults { get; set; } = [];
}

public class QuizAnswerResultResponse
{
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
