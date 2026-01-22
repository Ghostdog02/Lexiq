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
    public object UserExerciseProgress { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        OverrideMicrostIdentityTablesNames(modelBuilder);

        DefineRelationships(modelBuilder);
    }

    public void DefineRelationships(ModelBuilder modelBuilder)
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
            .Entity<MultipleChoiceExercise>()
            .HasMany(e => e.Options)
            .WithOne(eo => eo.Exercise as MultipleChoiceExercise)
            .HasForeignKey(eo => eo.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<UserExerciseProgress>()
            .HasIndex(p => new { p.UserId, p.ExerciseId })
            .IsUnique();

        modelBuilder
            .Entity<UserLessonProgress>()
            .HasIndex(p => new { p.UserId, p.LessonId })
            .IsUnique();

        modelBuilder
            .Entity<UserCourseProgress>()
            .HasIndex(p => new { p.UserId, p.CourseId })
            .IsUnique();

        modelBuilder.Entity<UserLanguage>().HasKey(ul => new { ul.UserId, ul.LanguageId });
    }

    public void OverrideMicrostIdentityTablesNames(ModelBuilder modelBuilder)
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
