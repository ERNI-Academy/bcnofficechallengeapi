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
            entity.HasIndex(e => new { e.UserId, e.SponsorId }).IsUnique();
        });
    }
}
