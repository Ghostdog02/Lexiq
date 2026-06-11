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
/// Integration tests verifying that hearts=0 blocks lesson submission for regular users,
/// while Admin/ContentCreator bypass roles can still submit.
/// </summary>
public class HeartsBlockSubmissionTests(DatabaseFixture fixture)
    : IClassFixture<DatabaseFixture>,
        IAsyncLifetime
{
    private readonly DatabaseFixture _fixture = fixture;
    private BackendDbContext _ctx = null!;
    private LessonProgressService _sut = null!;
    private FakeClock _clock = null!;
    private HeartsService _heartsService = null!;
    private UserManager<User> _userManager = null!;

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

        await EnsureRolesExistAsync();
        _sut = BuildService(_ctx);
    }

    public async ValueTask DisposeAsync()
    {
        await _ctx.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private async Task EnsureRolesExistAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_ctx);
        services.AddIdentityCore<User>().AddRoles<IdentityRole>().AddEntityFrameworkStores<BackendDbContext>();
        var sp = services.BuildServiceProvider();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in new[] { "Admin", "ContentCreator", "Student" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private LessonProgressService BuildService(BackendDbContext ctx)
    {
        return new LessonProgressService(
            ctx,
            new LessonService(ctx, new ExerciseService(ctx, new Moq.Mock<IFileUploadsService>().Object), _clock, new MemoryCache(new MemoryCacheOptions())),
            _userManager,
            new AchievementService(ctx),
            _clock,
            _heartsService
        );
    }

    private async Task<(string UserId, string ExerciseId, string CorrectOptionId, string WrongOptionId)>
        SeedAsync(string username, int hearts = 0, string? role = null)
    {
        var exerciseId = Guid.NewGuid().ToString();
        var correctId = Guid.NewGuid().ToString();
        var wrongId = Guid.NewGuid().ToString();
        await DbSeeder.CreateFillInBlankExerciseWithOptionsAsync(_ctx, _fixture.LessonId, exerciseId, correctId, wrongId);

        var user = new UserBuilder().WithUserName(username).WithEmail($"{username}@test.com").Build();
        user.Hearts = hearts;
        user.LastHeartResetAt = _clock.UtcNow;
        await DbSeeder.AddUserAsync(_ctx, user);

        if (role != null)
            await _userManager.AddToRoleAsync(user, role);

        _ctx.UserLessonProgress.Add(new UserLessonProgress { UserId = user.Id, LessonId = _fixture.LessonId, IsLocked = false });
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        return (user.Id, exerciseId, correctId, wrongId);
    }

    [Fact]
    public async Task Hearts0_WrongAnswer_Throws_NoHearts()
    {
        // Arrange
        var (userId, exerciseId, _, wrongId) = await SeedAsync("zeroWrong");

        // Act
        var act = () => _sut.SubmitLessonAsync(
            userId, _fixture.LessonId,
            [new ExerciseAnswerDto(exerciseId, wrongId)]
        );

        // Assert
        await act.Should().ThrowAsync<NoHeartsException>(
            because: "hearts=0 blocks all submissions regardless of answer"
        );
    }

    [Fact]
    public async Task Hearts0_CorrectAnswer_StillThrows_NoHearts()
    {
        // Arrange
        var (userId, exerciseId, correctId, _) = await SeedAsync("zeroCorrect");

        // Act
        var act = () => _sut.SubmitLessonAsync(
            userId, _fixture.LessonId,
            [new ExerciseAnswerDto(exerciseId, correctId)]
        );

        // Assert
        await act.Should().ThrowAsync<NoHeartsException>(
            because: "hearts check fires before answer validation — correct answers are also blocked"
        );
    }

    [Fact]
    public async Task Hearts0_ThenRefill_NextSubmissionAllowed()
    {
        // Arrange
        var (userId, exerciseId, correctId, _) = await SeedAsync("refilltest", hearts: 0);
        var user = await _ctx.Users.FindAsync([userId], TestContext.Current.CancellationToken);

        // Advance clock 4h to trigger refill
        _clock.Advance(TimeSpan.FromHours(4));
        await _heartsService.RefillAndGetHeartsAsync(user!);

        // Act — now hearts should be 1
        var result = await _sut.SubmitLessonAsync(
            userId, _fixture.LessonId,
            [new ExerciseAnswerDto(exerciseId, correctId)]
        );

        // Assert
        result.Should().NotBeNull(because: "after refill, submission should succeed");
        result.HeartsRemaining.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task AdminUser_Hearts0_SubmissionAllowed()
    {
        // Arrange
        var (userId, exerciseId, correctId, _) = await SeedAsync("adminzero", hearts: 0, role: "Admin");

        // Act
        var act = () => _sut.SubmitLessonAsync(
            userId, _fixture.LessonId,
            [new ExerciseAnswerDto(exerciseId, correctId)]
        );

        // Assert
        await act.Should().NotThrowAsync(because: "Admin bypasses hearts check");
    }
}
