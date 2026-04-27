using Backend.Database.Entities;
using Backend.Database.Entities.Exercises;
using Backend.Database.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database;

public class BackendDbContext(DbContextOptions options)
    : IdentityDbContext<User, IdentityRole, string>(options)
{
    public DbSet<Language> Languages { get; set; }

    public DbSet<Course> Courses { get; set; }

    public DbSet<Lesson> Lessons { get; set; }

    public DbSet<Exercise> Exercises { get; set; }

    public DbSet<UserLanguage> UserLanguages { get; set; }

    public DbSet<UserExerciseProgress> UserExerciseProgress { get; set; }

    public DbSet<UserAvatar> UserAvatars { get; set; }

    public DbSet<AudioMatchPair> AudioMatchPairs { get; set; }

    public DbSet<ImageOption> ImageOptions { get; set; }

    public DbSet<Achievement> Achievements { get; set; }

    public DbSet<UserAchievement> UserAchievements { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        OverrideMicrostIdentityTablesNames(modelBuilder);

        DefineRelationships(modelBuilder);
    }

    public static void DefineRelationships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserLanguage>(entity =>
        {
            entity.HasKey(ul => new { ul.UserId, ul.LanguageId });

            entity
                .HasOne(ul => ul.User)
                .WithMany(u => u.UserLanguages)
                .HasForeignKey(ul => ul.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(ul => ul.Language)
                .WithMany(l => l.UserLanguages)
                .HasForeignKey(ul => ul.LanguageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder
            .Entity<Language>()
            .HasMany(l => l.Courses)
            .WithOne(c => c.Language)
            .HasForeignKey(c => c.LanguageId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<Course>()
            .HasMany(c => c.Lessons)
            .WithOne(l => l.Course)
            .HasForeignKey(l => l.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<Lesson>()
            .HasMany(l => l.Exercises)
            .WithOne(e => e.Lesson)
            .HasForeignKey(e => e.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<ListeningExercise>()
            .HasMany(e => e.Options)
            .WithOne(eo => eo.Exercise as ListeningExercise)
            .HasForeignKey(eo => eo.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<ImageChoiceExercise>()
            .HasMany(e => e.Options)
            .WithOne(io => io.Exercise)
            .HasForeignKey(io => io.ImageChoiceExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<AudioMatchingExercise>()
            .HasMany(e => e.Pairs)
            .WithOne(p => p.Exercise)
            .HasForeignKey(p => p.AudioMatchingExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FillInBlankExercise>();
        modelBuilder.Entity<TrueFalseExercise>();
        modelBuilder.Entity<ImageChoiceExercise>();
        modelBuilder.Entity<AudioMatchingExercise>();

        modelBuilder.Entity<UserLanguage>().HasKey(ul => new { ul.UserId, ul.LanguageId });

        modelBuilder.Entity<UserAvatar>(entity =>
        {
            entity.HasKey(a => a.UserId);

            entity.Property(a => a.Data).HasColumnType("varbinary(max)");
            entity.Property(a => a.ContentType).HasMaxLength(50);

            entity
                .HasOne(a => a.User)
                .WithOne(u => u.Avatar)
                .HasForeignKey<UserAvatar>(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserExerciseProgress>(entity =>
        {
            entity.HasKey(uep => new { uep.UserId, uep.ExerciseId });

            entity
                .HasOne(uep => uep.User)
                .WithMany(u => u.ExerciseProgress)
                .HasForeignKey(uep => uep.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(uep => uep.Exercise)
                .WithMany(e => e.ExerciseProgress)
                .HasForeignKey(uep => uep.ExerciseId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Achievement>(entity =>
        {
            entity.Property(a => a.AchievementName).HasMaxLength(100).IsRequired();
            entity.Property(a => a.Description).HasMaxLength(500).IsRequired();
            entity.Property(a => a.Icon).HasMaxLength(10).IsRequired();
        });

        modelBuilder.Entity<UserAchievement>(entity =>
        {
            entity.HasKey(ua => new { ua.UserId, ua.AchievementId });

            entity
                .HasOne(ua => ua.User)
                .WithMany()
                .HasForeignKey(ua => ua.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(ua => ua.Achievement)
                .WithMany(a => a.UserAchievements)
                .HasForeignKey(ua => ua.AchievementId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public static void OverrideMicrostIdentityTablesNames(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdentityRole>(entity =>
        {
            entity.ToTable(name: "Roles");
        });

        modelBuilder.Entity<IdentityUserRole<string>>(entity =>
        {
            entity.ToTable("UserRoles");
        });

        modelBuilder.Entity<IdentityUserClaim<string>>(entity =>
        {
            entity.ToTable("UserClaims");
        });

        modelBuilder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.ToTable("UserLogins");
        });

        modelBuilder.Entity<IdentityRoleClaim<string>>(entity =>
        {
            entity.ToTable("RoleClaims");
        });

        modelBuilder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.ToTable("UserTokens");
        });

        modelBuilder.Entity<IdentityRoleClaim<string>>(entity =>
        {
            entity.ToTable("RoleClaims");
        });

        modelBuilder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.ToTable("UserTokens");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
        });
    }
}
