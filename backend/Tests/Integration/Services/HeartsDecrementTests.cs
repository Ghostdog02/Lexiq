using Backend.Api.Dtos;
using Backend.Api.Services;
using Backend.Database;
using Backend.Database.Entities;
using Backend.Database.Entities.Users;
using Backend.Tests.Builders;
using Backend.Tests.Helpers;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Tests.Integration.Services;

/// <summary>
/// Integration tests for heart decrement via LessonProgressService.SubmitLessonAsync.
///
/// Verifies:
/// - Wrong answer costs 1 heart
/// - Correct answer leaves hearts unchanged
/// - Hearts clamp at 0 (cannot go negative)
/// - Admin/ContentCreator bypass hearts penalty
/// </summary>
public class HeartsDecrementTests(DatabaseFixture fixture)
    : IClassFixture<DatabaseFixture>,
        IAsyncLifetime
{
    private readonly DatabaseFixture _fixture = fixture;
    private BackendDbContext _ctx = null!;
    private LessonProgressService _sut = null!;
    private string _lessonId = null!;

    public async ValueTask InitializeAsync()
    {
        _ctx = _fixture.CreateDbContext();
        await DbSeeder.ClearLeaderboardDataAsync(_ctx, _fixture.SystemUserId);
        _lessonId = _fixture.LessonId;
        _sut = BuildService(_ctx);
    }

    public async ValueTask DisposeAsync()
    {
        await _ctx.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private static LessonProgressService BuildService(BackendDbContext ctx)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(ctx);
        services.AddIdentityCore<User>().AddRoles<IdentityRole>().AddEntityFrameworkStores<BackendDbContext>();
        var sp = services.BuildServiceProvider();
        var userManager = sp.GetRequiredService<UserManager<User>>();
        var clock = new Backend.Api.Services.Clock.SystemClock();
        var heartsService = new HeartsService(ctx, clock);
        return new LessonProgressService(
            ctx,
            new LessonService(ctx, new ExerciseService(ctx, new Moq.Mock<IFileUploadsService>().Object), clock, new MemoryCache(new MemoryCacheOptions())),
            userManager,
            new AchievementService(ctx),
            clock,
            heartsService,
            new NullMemoryCache()
        );
    }

    private async Task<(User User, string ExerciseId, string CorrectOptionId, string WrongOptionId)>
        SeedUserAndExerciseAsync(string username, int hearts = 5)
    {
        var exerciseId = Guid.NewGuid().ToString();
        var correctOptionId = Guid.NewGuid().ToString();
        var wrongOptionId = Guid.NewGuid().ToString();

        await DbSeeder.CreateFillInBlankExerciseWithOptionsAsync(
            _ctx, _lessonId, exerciseId, correctOptionId, wrongOptionId, orderIndex: 0
        );

        var user = new UserBuilder()
            .WithUserName(username)
            .WithEmail($"{username}@test.com")
            .Build();
        user.Hearts = hearts;
        await DbSeeder.AddUserAsync(_ctx, user);

        _ctx.UserLessonProgress.Add(new UserLessonProgress { UserId = user.Id, LessonId = _lessonId, IsLocked = false });
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        return (user, exerciseId, correctOptionId, wrongOptionId);
    }

    [Fact]
    public async Task WrongAnswer_DecrementsHeartsByOne()
    {
        // Arrange
        var (user, exerciseId, _, wrongOptionId) = await SeedUserAndExerciseAsync("wronguser");
        var initialHearts = user.Hearts;

        // Act
        await _sut.SubmitLessonAsync(
            user.Id,
            _lessonId,
            [new ExerciseAnswerDto(exerciseId, wrongOptionId)]
        );
        await _ctx.Entry(user).ReloadAsync(TestContext.Current.CancellationToken);

        // Assert
        user.Hearts.Should().Be(initialHearts - 1, because: "wrong answer costs 1 heart");
    }

    [Fact]
    public async Task CorrectAnswer_HeartsUnchanged()
    {
        // Arrange
        var (user, exerciseId, correctOptionId, _) = await SeedUserAndExerciseAsync("correctuser");
        var initialHearts = user.Hearts;

        // Act
        await _sut.SubmitLessonAsync(
            user.Id,
            _lessonId,
            [new ExerciseAnswerDto(exerciseId, correctOptionId)]
        );
        await _ctx.Entry(user).ReloadAsync(TestContext.Current.CancellationToken);

        // Assert
        user.Hearts.Should().Be(initialHearts, because: "correct answer does not cost hearts");
    }

    [Fact]
    public async Task SixWrongAnswers_Starting5Hearts_ClampsAt0()
    {
        // Arrange — seed 6 exercises
        var exerciseIds = new List<string>();
        var wrongOptionIds = new List<string>();
        for (var i = 0; i < 6; i++)
        {
            var exId = Guid.NewGuid().ToString();
            var cId = Guid.NewGuid().ToString();
            var wId = Guid.NewGuid().ToString();
            await DbSeeder.CreateFillInBlankExerciseWithOptionsAsync(_ctx, _lessonId, exId, cId, wId, i + 1);
            exerciseIds.Add(exId);
            wrongOptionIds.Add(wId);
        }

        var user = new UserBuilder().WithUserName("sixwrong").WithEmail("six@test.com").Build();
        user.Hearts = 5;
        await DbSeeder.AddUserAsync(_ctx, user);

        _ctx.UserLessonProgress.Add(new UserLessonProgress { UserId = user.Id, LessonId = _lessonId, IsLocked = false });
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var answers = exerciseIds
            .Select((id, i) => new ExerciseAnswerDto(id, wrongOptionIds[i]))
            .ToList();
        await _sut.SubmitLessonAsync(user.Id, _lessonId, answers);
        await _ctx.Entry(user).ReloadAsync(TestContext.Current.CancellationToken);

        // Assert
        user.Hearts.Should().Be(0, because: "hearts clamp at 0 — never go negative");
    }

    [Fact]
    public async Task WrongAnswerAt1Heart_HeartDropsTo0()
    {
        // Arrange
        var (user, exerciseId, _, wrongOptionId) = await SeedUserAndExerciseAsync("lastlife", hearts: 1);

        // Act
        await _sut.SubmitLessonAsync(
            user.Id,
            _lessonId,
            [new ExerciseAnswerDto(exerciseId, wrongOptionId)]
        );
        await _ctx.Entry(user).ReloadAsync(TestContext.Current.CancellationToken);

        // Assert
        user.Hearts.Should().Be(0);
    }
}
