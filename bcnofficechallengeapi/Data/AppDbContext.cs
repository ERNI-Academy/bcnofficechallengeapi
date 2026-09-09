using bcnofficechallengeapi.Models;
using Microsoft.EntityFrameworkCore;

namespace bcnofficechallengeapi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<WeatherForecastRecord> WeatherForecastRecords => Set<WeatherForecastRecord>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Sponsor> Sponsors => Set<Sponsor>();

    public DbSet<RankingEntry> Ranking => Set<RankingEntry>();

    public DbSet<Credential> Credentials => Set<Credential>();

    public DbSet<UserSponsorScan> UserSponsorScans => Set<UserSponsorScan>();

    public DbSet<UserSponsorScanAnswer> UserSponsorScanAnswers => Set<UserSponsorScanAnswer>();

    public DbSet<Question> Questions => Set<Question>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.Email).HasColumnName("email").IsRequired();
            entity.Property(e => e.CompanyName).HasColumnName("company_name");
            entity.Property(e => e.JobTitle).HasColumnName("job_title");
            entity.Property(e => e.Points).HasColumnName("points");
            entity.Property(e => e.PointsTimestamp).HasColumnName("points_timestamp");
            entity.Property(e => e.LinkedIn).HasColumnName("linkedin");
            entity.Property(e => e.Password).HasColumnName("password").IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<Sponsor>(entity =>
        {
            entity.ToTable("sponsors");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.QrId).HasColumnName("qr_id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasColumnType("nvarchar(max)");
            entity.Property(e => e.Url).HasColumnName("url");
            entity.Property(e => e.ImageUrl).HasColumnName("image");
            entity.Property(e => e.PointsValue).HasColumnName("points_value");
        });

        modelBuilder.Entity<RankingEntry>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("ranking");
            entity.Property(e => e.UserId).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Points).HasColumnName("points");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.PointsTimestamp).HasColumnName("points_timestamp");
        });

        modelBuilder.Entity<Credential>(entity =>
        {
            entity.ToTable("credentials");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AppUser).HasColumnName("app_user").IsRequired();
            entity.Property(e => e.AppPass).HasColumnName("app_pass").IsRequired();
        });

        modelBuilder.Entity<UserSponsorScan>(entity =>
        {
            entity.ToTable("user_sponsor_scans");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.SponsorId).HasColumnName("sponsor_id");
            entity.Property(e => e.ScannedAt).HasColumnName("scanned_at");
            entity.Property(e => e.PointsAwarded).HasColumnName("points_awarded");
            entity.Property(e => e.MaximumPoints).HasColumnName("maximum_points");
            entity.HasIndex(e => new { e.UserId, e.SponsorId }).IsUnique();
            entity.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Sponsor>().WithMany().HasForeignKey(e => e.SponsorId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserSponsorScanAnswer>(entity =>
        {
            entity.ToTable("user_sponsor_scan_answers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.UserSponsorScanId).HasColumnName("user_sponsor_scan_id");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.QuestionText).HasColumnName("question_text").HasMaxLength(1000).IsRequired();
            entity.Property(e => e.SelectedAnswer).HasColumnName("selected_answer");
            entity.Property(e => e.CorrectAnswer).HasColumnName("correct_answer");
            entity.Property(e => e.IsCorrect).HasColumnName("is_correct");
            entity.Property(e => e.PointsAwarded).HasColumnName("points_awarded");
            entity.HasIndex(e => new { e.UserSponsorScanId, e.QuestionId }).IsUnique();
            entity.HasOne<UserSponsorScan>()
                .WithMany(scan => scan.AnswerResults)
                .HasForeignKey(e => e.UserSponsorScanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.ToTable("questions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.SponsorId).HasColumnName("sponsor_id");
            entity.Property(e => e.Text).HasColumnName("text").HasMaxLength(1000).IsRequired();
            entity.Property(e => e.CorrectAnswer).HasColumnName("correct_answer");
            entity.Property(e => e.Points).HasColumnName("points");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.HasIndex(e => new { e.SponsorId, e.SortOrder });
            entity.HasOne<Sponsor>().WithMany().HasForeignKey(e => e.SponsorId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
