using Backend.Database;
using Backend.Database.Entities;
using Backend.Database.Entities.Exercises;
using Backend.Database.Entities.Users;
using Backend.Tests.Builders;
using Backend.Tests.Helpers;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>
/// Tests for EF Core edge cases: TPH discriminator behavior, cascade delete prevention,
/// navigation property loading patterns.
/// </summary>
public class EfCoreEdgeCaseTests(DatabaseFixture fixture)
    : IClassFixture<DatabaseFixture>,
        IAsyncLifetime
{
    private readonly DatabaseFixture _fixture = fixture;
    private BackendDbContext _ctx = null!;
    private string _testUserId = null!;

    public async ValueTask InitializeAsync()
    {
        _ctx = _fixture.CreateDbContext();
        await DbSeeder.ClearLeaderboardDataAsync(_ctx, _fixture.SystemUserId);

        // Create test user
        var user = new UserBuilder().WithUserName("eftest").WithEmail("ef@test.com").Build();
        await DbSeeder.AddUserAsync(_ctx, user);
        _testUserId = user.Id;
    }

    public async ValueTask DisposeAsync()
    {
        await _ctx.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Verifies that EF Core's Table-Per-Hierarchy correctly filters exercises by discriminator
    /// when querying specific derived types.
    /// </summary>
    [Fact]
    public async Task TphDiscriminator_QuerySpecificType_ReturnsOnlyThatType()
    {
        // Arrange
        var fillInBlankId = await DbSeeder.CreateFillInBlankExerciseAsync(
            _ctx,
            _fixture.LessonId,
            orderIndex: 0,
            isLocked: false
        );
        var multipleChoiceId = await DbSeeder.CreateMultipleChoiceExerciseAsync(
            _ctx,
            _fixture.LessonId,
            orderIndex: 1,
            isLocked: false
        );
        var listeningId = await DbSeeder.CreateListeningExerciseAsync(
            _ctx,
            _fixture.LessonId,
            orderIndex: 2,
            isLocked: false
        );

        // Act - Query only MultipleChoiceExercise types via OfType<T>()
        var multipleChoiceExercises = await _ctx
            .Exercises.OfType<MultipleChoiceExercise>()
            .Where(e => e.LessonId == _fixture.LessonId)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        multipleChoiceExercises
            .Should()
            .ContainSingle(
                because: "OfType<MultipleChoiceExercise>() should filter by TPH discriminator and return only that type"
            );
        multipleChoiceExercises[0]
            .Id.Should()
            .Be(
                multipleChoiceId,
                because: "the returned exercise should be the MultipleChoice one we created"
            );
    }

    /// <summary>
    /// Verifies that EF Core correctly loads child collections for polymorphic types
    /// using ThenInclude with type casting (e.g., .ThenInclude(e => (e as MultipleChoiceExercise)!.Options)).
    /// </summary>
    [Fact]
    public async Task TphPolymorphicInclude_LoadsChildCollections_ForSpecificTypes()
    {
        // Arrange
        var multipleChoiceId = await DbSeeder.CreateMultipleChoiceExerciseAsync(
            _ctx,
            _fixture.LessonId,
            orderIndex: 0,
            isLocked: false
        );

        // Clear context to force fresh query
        _ctx.ChangeTracker.Clear();

        // Act - Load exercises with polymorphic ThenInclude
        var lesson = await _ctx
            .Lessons.Include(l => l.Exercises)
            .ThenInclude(e => (e as MultipleChoiceExercise)!.Options)
            .FirstAsync(l => l.Id == _fixture.LessonId, TestContext.Current.CancellationToken);

        var multipleChoiceExercise = lesson
            .Exercises.OfType<MultipleChoiceExercise>()
            .FirstOrDefault(e => e.Id == multipleChoiceId);

        // Assert
        multipleChoiceExercise.Should().NotBeNull();
        multipleChoiceExercise!
            .Options.Should()
            .NotBeEmpty(
                because: "ThenInclude with type cast should eagerly load Options for MultipleChoiceExercise"
            );
        multipleChoiceExercise
            .Options.Should()
            .HaveCount(
                3,
                because: "DbSeeder.CreateMultipleChoiceExerciseAsync creates 3 options by default"
            );
        multipleChoiceExercise
            .Options.Should()
            .OnlyContain(
                o => o.ExerciseId == multipleChoiceId,
                because: "all options should reference the parent exercise"
            );
    }

    /// <summary>
    /// Verifies that UserExerciseProgress.ExerciseId FK uses DeleteBehavior.NoAction
    /// to prevent multiple cascade paths (SQL Server constraint).
    /// Deleting a User cascades to UserExerciseProgress, but deleting an Exercise does NOT.
    /// EF Core throws InvalidOperationException when trying to modify a key property during cascade.
    /// </summary>
    [Fact]
    public async Task UserExerciseProgress_ExerciseFk_UsesNoActionDelete()
    {
        // Arrange
        var exerciseId = await DbSeeder.CreateFillInBlankExerciseAsync(
            _ctx,
            _fixture.LessonId,
            orderIndex: 0,
            isLocked: false
        );

        await DbSeeder.AddProgressAsync(
            _ctx,
            _testUserId,
            exerciseId,
            isCompleted: true,
            pointsEarned: 10,
            completedAt: DateTime.UtcNow
        );

        // Arrange - load exercise
        var exercise = await _ctx.Exercises.FindAsync(
            new object[] { exerciseId },
            TestContext.Current.CancellationToken
        );

        // Act & Assert - Remove throws synchronously because EF Core's change tracker
        // immediately tries to cascade and cannot null out the composite key property
        Action removeAction = () => _ctx.Exercises.Remove(exercise!);

        removeAction
            .Should()
            .Throw<InvalidOperationException>(
                because: "DeleteBehavior.NoAction on UserExerciseProgress.ExerciseId prevents cascade delete from Exercise; EF Core throws when attempting to modify the composite key property during Remove"
            )
            .WithMessage(
                "*ExerciseId*part of a key*cannot be modified*",
                because: "the error should indicate that the key property cannot be modified during cascade"
            );
    }

    /// <summary>
    /// Verifies that adding child entities to a parent's navigation collection
    /// (rather than directly to DbContext) correctly populates navigation properties
    /// after SaveChangesAsync.
    /// </summary>
    [Fact]
    public async Task NavigationPropertyPopulation_AddToParentCollection_PopulatesAfterSave()
    {
        // Arrange
        var language = new Language
        {
            Id = Guid.NewGuid().ToString(),
            Name = "TestLanguage",
            FlagIconUrl = null,
            CreatedAt = DateTime.UtcNow,
            Courses = [],
        };
        _ctx.Languages.Add(language);

        var course = new Course
        {
            Id = Guid.NewGuid().ToString(),
            Title = "TestCourse",
            Description = "Description",
            LanguageId = language.Id,
            OrderIndex = 0,
            CreatedAt = DateTime.UtcNow,
            CreatedById = _fixture.SystemUserId,
            Lessons = [],
        };

        // Act - Add course to parent's navigation collection (NOT directly to DbContext)
        language.Courses.Add(course);
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        language
            .Courses.Should()
            .ContainSingle(
                because: "adding child to parent's navigation collection should populate it after SaveChangesAsync"
            );
        language
            .Courses[0]
            .Id.Should()
            .Be(course.Id, because: "the populated navigation should reference the added course");

        // Verify course was saved to database
        var savedCourse = await _ctx.Courses.FindAsync(
            new object[] { course.Id },
            TestContext.Current.CancellationToken
        );
        savedCourse.Should().NotBeNull(because: "course should be tracked and saved via parent");
        savedCourse!
            .LanguageId.Should()
            .Be(language.Id, because: "FK should be correctly set from parent relationship");
    }

    /// <summary>
    /// Verifies that a full cascade delete chain (Language → Course → Lesson → Exercise)
    /// completes successfully without leaving orphaned records.
    /// </summary>
    [Fact]
    public async Task CascadeDelete_FullHierarchy_DeletesAllRelatedEntities()
    {
        // Arrange
        var language = new Language
        {
            Id = Guid.NewGuid().ToString(),
            Name = "CascadeTestLanguage",
            FlagIconUrl = null,
            CreatedAt = DateTime.UtcNow,
            Courses = [],
        };

        var course = new Course
        {
            Id = Guid.NewGuid().ToString(),
            Title = "CascadeTestCourse",
            Description = "Description",
            LanguageId = language.Id,
            OrderIndex = 0,
            CreatedAt = DateTime.UtcNow,
            CreatedById = _fixture.SystemUserId,
            Lessons = [],
        };
        language.Courses.Add(course);

        var lesson = new Lesson
        {
            Id = Guid.NewGuid().ToString(),
            Title = "CascadeTestLesson",
            CourseId = course.Id,
            OrderIndex = 0,
            IsLocked = false,
            LessonContent = "{}",
            CreatedAt = DateTime.UtcNow,
            Exercises = [],
        };
        course.Lessons.Add(lesson);

        var exercise = new FillInBlankExercise
        {
            Id = Guid.NewGuid().ToString(),
            LessonId = lesson.Id,
            Title = "CascadeTestExercise",
            Text = "Question",
            CorrectAnswer = "Answer",
            AcceptedAnswers = "Answer",
            CaseSensitive = false,
            TrimWhitespace = true,
            Points = 10,
            DifficultyLevel = DifficultyLevel.Beginner,
            OrderIndex = 0,
            IsLocked = false,
            CreatedAt = DateTime.UtcNow,
        };
        lesson.Exercises.Add(exercise);

        _ctx.Languages.Add(language);
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - Delete the language (should cascade through entire hierarchy)
        _ctx.Languages.Remove(language);
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert - All related entities should be deleted
        var deletedLanguage = await _ctx.Languages.FindAsync(
            new object[] { language.Id },
            TestContext.Current.CancellationToken
        );
        var deletedCourse = await _ctx.Courses.FindAsync(
            new object[] { course.Id },
            TestContext.Current.CancellationToken
        );
        var deletedLesson = await _ctx.Lessons.FindAsync(
            new object[] { lesson.Id },
            TestContext.Current.CancellationToken
        );
        var deletedExercise = await _ctx.Exercises.FindAsync(
            new object[] { exercise.Id },
            TestContext.Current.CancellationToken
        );

        deletedLanguage.Should().BeNull(because: "language should be deleted from the database");
        deletedCourse
            .Should()
            .BeNull(
                because: "course should cascade delete when parent language is deleted (DeleteBehavior.Cascade)"
            );
        deletedLesson
            .Should()
            .BeNull(
                because: "lesson should cascade delete when parent course is deleted (DeleteBehavior.Cascade)"
            );
        deletedExercise
            .Should()
            .BeNull(
                because: "exercise should cascade delete when parent lesson is deleted (DeleteBehavior.Cascade)"
            );
    }

    /// <summary>
    /// Verifies that UserLanguage composite key (UserId, LanguageId) enforces uniqueness
    /// at the database level, preventing duplicate enrollments.
    /// </summary>
    [Fact]
    public async Task CompositeKey_Uniqueness_EnforcedByDatabase()
    {
        // Arrange
        var languageId = (
            await _ctx.Languages.FirstAsync(TestContext.Current.CancellationToken)
        ).Id;

        var enrollment1 = new UserLanguage
        {
            UserId = _testUserId,
            LanguageId = languageId,
            EnrolledAt = DateTime.UtcNow,
        };
        _ctx.UserLanguages.Add(enrollment1);
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Clear change tracker to bypass EF Core's identity map (test DB-level constraint)
        _ctx.ChangeTracker.Clear();

        var enrollment2 = new UserLanguage
        {
            UserId = _testUserId,
            LanguageId = languageId,
            EnrolledAt = DateTime.UtcNow.AddMinutes(1),
        };
        _ctx.UserLanguages.Add(enrollment2);

        // Act & Assert - Should throw DbUpdateException due to PK violation
        var action = async () => await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        await action
            .Should()
            .ThrowAsync<DbUpdateException>(
                because: "composite primary key (UserId, LanguageId) should prevent duplicate enrollments at the database level"
            );
    }
}
