using bcnofficechallengeapi.Data;
using bcnofficechallengeapi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace bcnofficechallengeapi.Pages.Backoffice;

[Authorize(AuthenticationSchemes = "BackofficeCookie")]
public class QuestionsModel(AppDbContext db) : PageModel
{
    public List<Sponsor> Rooms { get; set; } = [];
    public List<Question> Questions { get; set; } = [];

    [BindProperty]
    public List<QuestionForm> NewQuestions { get; set; } = [];

    [BindProperty]
    public QuestionForm Form { get; set; } = new();

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAddAsync()
    {
        var rows = NewQuestions.Where(row => !string.IsNullOrWhiteSpace(row.Text)).ToList();
        if (rows.Count == 0)
        {
            TempData["Error"] = "Add at least one question.";
            return RedirectToPage();
        }

        var roomIds = await db.Sponsors.Select(room => room.Id).ToHashSetAsync();
        if (rows.Any(row => !roomIds.Contains(row.SponsorId) || row.Points < 0))
        {
            TempData["Error"] = "Every question needs a valid room and a non-negative score.";
            return RedirectToPage();
        }

        var nextOrders = await db.Questions
            .GroupBy(question => question.SponsorId)
            .Select(group => new { SponsorId = group.Key, Next = group.Max(question => question.SortOrder) + 1 })
            .ToDictionaryAsync(item => item.SponsorId, item => item.Next);

        foreach (var row in rows)
        {
            var nextOrder = nextOrders.GetValueOrDefault(row.SponsorId);
            db.Questions.Add(new Question
            {
                SponsorId = row.SponsorId,
                Text = row.Text.Trim(),
                CorrectAnswer = row.CorrectAnswer,
                Points = row.Points,
                SortOrder = nextOrder
            });
            nextOrders[row.SponsorId] = nextOrder + 1;
        }

        await db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        var question = await db.Questions.FindAsync(Form.Id);
        if (question is null)
            return RedirectToPage();

        if (string.IsNullOrWhiteSpace(Form.Text) || Form.Points < 0 ||
            !await db.Sponsors.AnyAsync(room => room.Id == Form.SponsorId))
        {
            TempData["Error"] = "Question text, room and score are required.";
            return RedirectToPage();
        }

        question.SponsorId = Form.SponsorId;
        question.Text = Form.Text.Trim();
        question.CorrectAnswer = Form.CorrectAnswer;
        question.Points = Form.Points;
        await db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var question = await db.Questions.FindAsync(id);
        if (question is not null)
        {
            db.Questions.Remove(question);
            await db.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Rooms = await db.Sponsors.OrderBy(room => room.Name).ToListAsync();
        Questions = await db.Questions
            .OrderBy(question => question.SponsorId)
            .ThenBy(question => question.SortOrder)
            .ToListAsync();
    }
}

public class QuestionForm
{
    public Guid Id { get; set; }
    public Guid SponsorId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool CorrectAnswer { get; set; }
    public int Points { get; set; }
}
