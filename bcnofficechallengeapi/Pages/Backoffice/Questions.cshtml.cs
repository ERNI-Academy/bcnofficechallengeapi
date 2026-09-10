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
    public QuestionForm NewQuestion { get; set; } = QuestionForm.CreateDefault();

    [BindProperty]
    public QuestionForm Form { get; set; } = new();

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAddAsync()
    {
        var validationError = ValidateForm(NewQuestion, out var options);
        if (validationError is not null)
        {
            TempData["Error"] = validationError;
            return RedirectToPage();
        }

        if (!await db.Sponsors.AnyAsync(room => room.Id == NewQuestion.SponsorId))
        {
            TempData["Error"] = "Select a valid room.";
            return RedirectToPage();
        }

        if (await db.Questions.AnyAsync(question => question.SponsorId == NewQuestion.SponsorId))
        {
            TempData["Error"] = "This room already has a question.";
            return RedirectToPage();
        }

        db.Questions.Add(CreateQuestion(NewQuestion, options));
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "This room already has a question.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        var validationError = ValidateForm(Form, out var options);
        if (validationError is not null)
        {
            TempData["Error"] = validationError;
            return RedirectToPage();
        }

        var question = await db.Questions
            .Include(item => item.Options)
            .SingleOrDefaultAsync(item => item.Id == Form.Id);
        if (question is null)
            return RedirectToPage();

        if (!await db.Sponsors.AnyAsync(room => room.Id == Form.SponsorId))
        {
            TempData["Error"] = "Select a valid room.";
            return RedirectToPage();
        }

        if (await db.Questions.AnyAsync(item => item.SponsorId == Form.SponsorId && item.Id != Form.Id))
        {
            TempData["Error"] = "This room already has a question.";
            return RedirectToPage();
        }

        question.SponsorId = Form.SponsorId;
        question.Text = Form.Text.Trim();
        question.Points = Form.Points;

        db.QuestionOptions.RemoveRange(question.Options);
        question.Options = options
            .Select((option, index) => new QuestionOption
            {
                Id = Guid.NewGuid(),
                Text = option.Text,
                IsCorrect = option.IsCorrect,
                SortOrder = index
            })
            .ToList();
        db.QuestionOptions.AddRange(question.Options);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Could not save the question.";
        }

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
            .Include(question => question.Options)
            .OrderBy(question => question.SponsorId)
            .ToListAsync();
    }

    private static Question CreateQuestion(QuestionForm form, IReadOnlyList<NormalizedOption> options) => new()
    {
        Id = Guid.NewGuid(),
        SponsorId = form.SponsorId,
        Text = form.Text.Trim(),
        Points = form.Points,
        Options = options
            .Select((option, index) => new QuestionOption
            {
                Id = Guid.NewGuid(),
                Text = option.Text,
                IsCorrect = option.IsCorrect,
                SortOrder = index
            })
            .ToList()
    };

    private static string? ValidateForm(QuestionForm? form, out List<NormalizedOption> options)
    {
        options = [];
        if (form is null || string.IsNullOrWhiteSpace(form.Text))
            return "Question text is required.";
        if (form.Text.Trim().Length > 1000)
            return "Question text cannot exceed 1000 characters.";
        if (form.Points < 0)
            return "Points cannot be negative.";

        var postedOptions = form.Options ?? [];
        if (postedOptions.Count < 2)
            return "Add at least two answer options.";
        if (postedOptions.Any(option => string.IsNullOrWhiteSpace(option.Text)))
            return "Every answer option needs text.";

        options = postedOptions
            .Select(option => new NormalizedOption(option.Text.Trim(), option.IsCorrect))
            .ToList();
        if (options.Any(option => option.Text.Length > 1000))
            return "Answer options cannot exceed 1000 characters.";
        if (options.Select(option => option.Text).Distinct(StringComparer.OrdinalIgnoreCase).Count() != options.Count)
            return "Answer options must be unique.";
        if (!options.Any(option => option.IsCorrect))
            return "Mark at least one answer as correct.";

        return null;
    }

    private sealed record NormalizedOption(string Text, bool IsCorrect);
}

public class QuestionForm
{
    public Guid Id { get; set; }
    public Guid SponsorId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Points { get; set; } = 1;
    public List<QuestionOptionForm> Options { get; set; } = [];

    public static QuestionForm CreateDefault() => new()
    {
        Options = [new QuestionOptionForm(), new QuestionOptionForm()]
    };
}

public class QuestionOptionForm
{
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
