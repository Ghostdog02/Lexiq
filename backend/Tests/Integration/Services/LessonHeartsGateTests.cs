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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Tests.Integration.Services;

/// <summary>
/// Integration tests for the hearts-based lesson access gate.
///
/// Hearts replace the old 70 % XP unlock threshold:
/// - Hearts > 0: any lesson accessible and submittable
/// - Hearts = 0: block with NoHeartsException
/// - UnlockNextLessonAsync is never called after submit
/// - Admin/ContentCreator bypass the hearts check
/// </summary>
public class LessonHeartsGateTests(DatabaseFixture fixture)
    : IClassFixture<DatabaseFixture>,
        IAsyncLifetime
{
    private readonly DatabaseFixture _fixture = fixture;
    private BackendDbContext _ctx = null!;
    private LessonProgressService _sut = null!;
    private FakeClock _clock = null!;
    private HeartsService _heartsService = null!;
    private UserManager<User> _userManager = null!;
    private string _exerciseId = null!;
    private string _correctOptionId = null!;
    private string _wrongOptionId = null!;

    public async ValueTask InitializeAsync()
    {
        _ctx = _fixture.CreateDbContext();
        await DbSeeder.ClearLeaderboardDataAsync(_ctx, _fixture.SystemUserId);
        _clock = new FakeClock();
        _heartsService = new HeartsService(_ctx, _clock);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_ctx);
        services.AddIdentityCore<User>().AddRoles<IdentityRole>().AddEntityFrameworkStores<BackendDbContext>();
        var sp = services.BuildServiceProvider();
        _userManager = sp.GetRequiredService<UserManager<User>>();

        await EnsureRolesAsync(sp);

        _sut = BuildService();

        // Seed a single exercise in the fixture lesson
        _exerciseId = Guid.NewGuid().ToString();
        _correctOptionId = Guid.NewGuid().ToString();
        _wrongOptionId = Guid.NewGuid().ToString();
        await DbSeeder.CreateFillInBlankExerciseWithOptionsAsync(
            _ctx, _fixture.LessonId, _exerciseId, _correctOptionId, _wrongOptionId
        );
    }

    public async ValueTask DisposeAsync()
    {
        await _ctx.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private static async Task EnsureRolesAsync(IServiceProvider sp)
    {
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Admin", "ContentCreator", "Student" })
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
    }

    private LessonProgressService BuildService() => new(
        _ctx,
        new LessonService(
            _ctx,
            new ExerciseService(_ctx, new Moq.Mock<IFileUploadsService>().Object),
            _clock,
            new MemoryCache(new MemoryCacheOptions())
        ),
        _userManager,
        new AchievementService(_ctx),
        _clock,
        _heartsService
    );

    private async Task<User> CreateUserAsync(string username, int hearts, string? role = null)
    {
        var user = new UserBuilder()
            .WithUserName(username)
            .WithEmail($"{username}@test.com")
            .Build();
        user.Hearts = hearts;
        user.LastHeartResetAt = _clock.UtcNow;
        await DbSeeder.AddUserAsync(_ctx, user);
        if (role != null)
            await _userManager.AddToRoleAsync(user, role);

        _ctx.UserLessonProgress.Add(new UserLessonProgress { UserId = user.Id, LessonId = _fixture.LessonId, IsLocked = false });
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        return user;
    }

    [Fact]
    public async Task NewUser_5Hearts_CanSubmitAnyLesson()
    {
        // Arrange
        var user = await CreateUserAsync("newuser5", 5);

        // Act
        var result = await _sut.SubmitLessonAsync(
            user.Id, _fixture.LessonId,
            [new ExerciseAnswerDto(_exerciseId, _correctOptionId)]
        );

        // Assert
        result.Should().NotBeNull(because: "5 hearts — lesson submission must succeed");
        result.HeartsRemaining.Should().Be(5, because: "correct answer does not cost hearts");
    }

    [Fact]
    public async Task Hearts0_SubmitThrowsNoHeartsException()
    {
        // Arrange
        var user = await CreateUserAsync("nohearts", 0);

        // Act
        var act = () => _sut.SubmitLessonAsync(
            user.Id, _fixture.LessonId,
            [new ExerciseAnswerDto(_exerciseId, _correctOptionId)]
        );

        // Assert
        await act.Should().ThrowAsync<NoHeartsException>(
            because: "hearts=0 blocks all submissions regardless of correctness"
        );
    }

    [Fact]
    public async Task Hearts0_Submit_NoDB_Write_NoXpAwarded()
    {
        // Arrange
        var user = await CreateUserAsync("noxpuser", 0);
        var xpBefore = user.TotalPointsEarned;

        // Act
        try
        {
            await _sut.SubmitLessonAsync(
                user.Id, _fixture.LessonId,
                [new ExerciseAnswerDto(_exerciseId, _correctOptionId)]
            );
        }
        catch (NoHeartsException) { /* expected */ }

        await _ctx.Entry(user).ReloadAsync(TestContext.Current.CancellationToken);

        // Assert
        user.TotalPointsEarned.Should().Be(xpBefore, because: "blocked submission must not award XP");
        var progress = await _ctx.UserExerciseProgress
            .FirstOrDefaultAsync(p => p.UserId == user.Id && p.ExerciseId == _exerciseId,
                TestContext.Current.CancellationToken);
        progress.Should().BeNull(because: "no progress row should be written when blocked");
    }

    [Fact]
    public async Task HeartsDrop_Then_Refill_SubmissionSucceeds()
    {
        // Arrange — start at 1 heart, burn it
        var user = await CreateUserAsync("refill1", 1);
        await _sut.SubmitLessonAsync(
            user.Id, _fixture.LessonId,
            [new ExerciseAnswerDto(_exerciseId, _wrongOptionId)]
        );
        await _ctx.Entry(user).ReloadAsync(TestContext.Current.CancellationToken);
        user.Hearts.Should().Be(0);

        // Advance clock by 4h to trigger refill
        _clock.Advance(TimeSpan.FromHours(4));
        await _heartsService.RefillAndGetHeartsAsync(user);

        // Act — submit again
        var result = await _sut.SubmitLessonAsync(
            user.Id, _fixture.LessonId,
            [new ExerciseAnswerDto(_exerciseId, _correctOptionId)]
        );

        // Assert
        result.Should().NotBeNull(because: "after refill, submission succeeds");
    }

    [Fact]
    public async Task AllWrongAnswers_HeartsDecrementUntilZero_ThenNoHearts()
    {
        // Arrange — 5 hearts, submit 5 wrong answers in sequence
        var user = await CreateUserAsync("burnhearts", 5);
        for (var i = 0; i < 5; i++)
        {
            var exId = Guid.NewGuid().ToString();
            var cId = Guid.NewGuid().ToString();
            var wId = Guid.NewGuid().ToString();
            await DbSeeder.CreateFillInBlankExerciseWithOptionsAsync(_ctx, _fixture.LessonId, exId, cId, wId, i + 1);

            await _sut.SubmitLessonAsync(user.Id, _fixture.LessonId, [new ExerciseAnswerDto(exId, wId)]);
        }
        await _ctx.Entry(user).ReloadAsync(TestContext.Current.CancellationToken);
        user.Hearts.Should().Be(0);

        // 6th wrong answer — now blocked
        var act = () => _sut.SubmitLessonAsync(
            user.Id, _fixture.LessonId,
            [new ExerciseAnswerDto(_exerciseId, _wrongOptionId)]
        );

        // Assert
        await act.Should().ThrowAsync<NoHeartsException>(
            because: "after burning all 5 hearts, NoHeartsException is thrown"
        );
    }

    [Fact]
    public async Task NoUnlockNextLessonCalled_AfterSubmit()
    {
        // Arrange — all exercises correct, completing the lesson
        var user = await CreateUserAsync("nounlock", 5);

        // Act
        await _sut.SubmitLessonAsync(
            user.Id, _fixture.LessonId,
            [new ExerciseAnswerDto(_exerciseId, _correctOptionId)]
        );

        // Assert — verify that all lessons in the course remain at their seeded IsLocked state
        var courseId = await _ctx.Lessons
            .Where(x => x.LessonId == _fixture.LessonId)
            .Select(x => x.CourseId)
            .FirstAsync(TestContext.Current.CancellationToken);

        var allLessons = await _ctx.Lessons
            .Where(l => l.CourseId == courseId)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Since we removed IsLocked-by-progression, no lesson should be affected by submission
        // The key assertion: no lesson was newly locked or unlocked as a result of the submission
        allLessons.Should().AllSatisfy(l =>
            l.IsLocked.Should().BeFalse(
                because: "migration sets all lessons to IsLocked=false — progression no longer gates lessons"
            )
        );
    }

    [Fact]
    public async Task AdminUser_Hearts0_CanSubmit()
    {
        // Arrange
        var admin = await CreateUserAsync("adminzerohearts", 0, "Admin");

        // Act
        var act = () => _sut.SubmitLessonAsync(
            admin.Id, _fixture.LessonId,
            [new ExerciseAnswerDto(_exerciseId, _correctOptionId)]
        );

        // Assert
        await act.Should().NotThrowAsync(because: "Admin bypasses hearts gate");
    }

    [Fact]
    public async Task ContentCreator_Hearts0_CanSubmit()
    {
        // Arrange
        var creator = await CreateUserAsync("creatorzerohearts", 0, "ContentCreator");

        // Act
        var act = () => _sut.SubmitLessonAsync(
            creator.Id, _fixture.LessonId,
            [new ExerciseAnswerDto(_exerciseId, _correctOptionId)]
        );

        // Assert
        await act.Should().NotThrowAsync(because: "ContentCreator bypasses hearts gate");
    }
}
