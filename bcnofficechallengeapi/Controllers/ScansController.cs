using System.Data;
using System.Security.Claims;
using System.Text.Json;
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
    public async Task<ActionResult<PreparedQuizResponse>> Prepare(PrepareQuestionsRequest request)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var resolved = await ResolveRoomAsync(request.RoomId, request.Qr);
        if (!resolved.Ok)
            return StatusCode(resolved.Status, new { error = resolved.Error });

        if (await HasCompletedAsync(userId, resolved.Sponsor!.Id))
            return Conflict(new { errorCode = "ROOM_ALREADY_COMPLETED", error = AlreadyCompletedMessage });

        var question = await db.Questions
            .Where(item => item.SponsorId == resolved.Sponsor.Id)
            .Include(item => item.Options)
            .SingleOrDefaultAsync();

        if (question is null)
            return BadRequest(new { error = "This room does not have any questions yet." });

        if (!IsValidQuestionConfiguration(question))
            return BadRequest(new { error = "This room has an invalid question configuration." });

        return Ok(new PreparedQuizResponse
        {
            RoomId = resolved.Sponsor.Id,
            RoomName = resolved.Sponsor.Name,
            Question = ToPreparedQuestion(question)
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

        var question = await db.Questions
            .Where(item => item.SponsorId == resolved.Sponsor.Id)
            .Include(item => item.Options)
            .SingleOrDefaultAsync();

        if (question is null)
            return BadRequest(new { error = "This room does not have any questions yet." });

        if (!IsValidQuestionConfiguration(question))
            return BadRequest(new { error = "This room has an invalid question configuration." });

        if (request.QuestionId == Guid.Empty || request.QuestionId != question.Id)
            return BadRequest(new { error = "This question does not belong to this room." });

        var selectedOptionIds = request.SelectedOptionIds ?? [];
        if (selectedOptionIds.Count == 0)
            return BadRequest(new { error = "At least one answer must be selected." });

        if (selectedOptionIds.Distinct().Count() != selectedOptionIds.Count)
            return BadRequest(new { error = "An answer cannot be selected more than once." });

        var optionsById = question.Options.ToDictionary(option => option.Id);
        if (selectedOptionIds.Any(optionId => !optionsById.ContainsKey(optionId)))
            return BadRequest(new { error = "One or more answers do not belong to this question." });

        var correctOptionIds = question.Options
            .Where(option => option.IsCorrect)
            .Select(option => option.Id)
            .ToHashSet();
        var selectedOptionIdSet = selectedOptionIds.ToHashSet();
        var isCorrect = selectedOptionIdSet.SetEquals(correctOptionIds);
        var orderedSelectedOptionIds = question.Options
            .Where(option => selectedOptionIdSet.Contains(option.Id))
            .OrderBy(option => option.SortOrder)
            .ThenBy(option => option.Id)
            .Select(option => option.Id)
            .ToList();
        var orderedCorrectOptionIds = question.Options
            .Where(option => correctOptionIds.Contains(option.Id))
            .OrderBy(option => option.SortOrder)
            .ThenBy(option => option.Id)
            .Select(option => option.Id)
            .ToList();

        var calculatedAnswer = new CalculatedQuizAnswer(
            question.Id,
            question.Text,
            orderedSelectedOptionIds,
            orderedCorrectOptionIds,
            isCorrect,
            isCorrect ? question.Points : 0);
        var completedAt = DateTime.UtcNow;

        var scan = new UserSponsorScan
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SponsorId = resolved.Sponsor.Id,
            ScannedAt = completedAt,
            PointsAwarded = calculatedAnswer.PointsAwarded,
            MaximumPoints = question.Points
        };

        db.UserSponsorScans.Add(scan);
        db.UserSponsorScanAnswers.Add(new UserSponsorScanAnswer
        {
            Id = Guid.NewGuid(),
            UserSponsorScanId = scan.Id,
            QuestionId = calculatedAnswer.QuestionId,
            QuestionText = calculatedAnswer.QuestionText,
            SelectedOptionIdsJson = JsonSerializer.Serialize(calculatedAnswer.SelectedOptionIds),
            CorrectOptionIdsJson = JsonSerializer.Serialize(calculatedAnswer.CorrectOptionIds),
            IsCorrect = calculatedAnswer.IsCorrect,
            PointsAwarded = calculatedAnswer.PointsAwarded
        });

        user.Points += calculatedAnswer.PointsAwarded;
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
            PointsEarned = calculatedAnswer.PointsAwarded,
            MaximumPoints = question.Points,
            TotalPoints = user.Points,
            CompletedAt = completedAt,
            AnswerResults = [new QuizAnswerResultResponse
            {
                QuestionId = calculatedAnswer.QuestionId,
                QuestionText = calculatedAnswer.QuestionText,
                IsCorrect = calculatedAnswer.IsCorrect
            }]
        });
    }

    private static bool IsValidQuestionConfiguration(Question question) =>
        question.Options.Count >= 2 && question.Options.Any(option => option.IsCorrect);

    private static PreparedQuestionResponse ToPreparedQuestion(Question question) => new()
    {
        Id = question.Id,
        Text = question.Text,
        Options = question.Options
            .OrderBy(option => option.SortOrder)
            .ThenBy(option => option.Id)
            .Select(option => new PreparedOptionResponse
            {
                Id = option.Id,
                Text = option.Text
            })
            .ToList()
    };

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
        List<Guid> SelectedOptionIds,
        List<Guid> CorrectOptionIds,
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
    public Guid QuestionId { get; set; }
    public List<Guid>? SelectedOptionIds { get; set; }
}

public class PreparedQuizResponse
{
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public PreparedQuestionResponse Question { get; set; } = new();
}

public class PreparedQuestionResponse
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public List<PreparedOptionResponse> Options { get; set; } = [];
}

public class PreparedOptionResponse
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
