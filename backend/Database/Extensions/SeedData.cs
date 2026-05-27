using Backend.Database.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Database.Extensions;

public class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BackendDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

            await SeedRolesAsync(context, userManager);
            var adminUserId = await SeedAdminUserAsync(context, userManager);

            await SeedContentAsync(context, adminUserId);
        }
        
        catch (DbUpdateException ex)
        {
            throw new Exception("A database error occurred while saving data.", ex);
        }

        catch (InvalidOperationException ex)
        {
            throw new Exception("An invalid operation occurred during user processing.", ex);
        }

        catch (ArgumentNullException ex)
        {
            throw new Exception("A null parameter has been passed during seeding roles", ex);
        }

        catch (Exception ex)
        {
            throw new Exception("An unexpected error occurred during user initialization.", ex);
        }
    }

    private static async Task<string> SeedAdminUserAsync(
        BackendDbContext context,
        UserManager<User> userManager
    )
    {
        const string adminEmail = "alex.vesely07@gmail.com";
        const string userName = "Ghostdog";

        var existingUser = await userManager.FindByEmailAsync(adminEmail);
        if (existingUser != null)
        {
            return existingUser.Id;
        }

        var today = DateTime.Today;

        var user = new User
        {
            UserName = userName,
            Email = adminEmail,
            EmailConfirmed = true,
            LockoutEnabled = false,
            LastLoginDate = today,
            RegistrationDate = today,
            NormalizedEmail = userManager.NormalizeEmail(adminEmail),
        };

        var creationResult = await userManager.CreateAsync(user);

        if (!creationResult.Succeeded)
        {
            throw new InvalidOperationException($"{creationResult.Errors}");
        }

        var addToRoleResult = await userManager.AddToRoleAsync(user, "Admin");

        if (!addToRoleResult.Succeeded)
        {
            throw new InvalidOperationException($"{addToRoleResult.Errors}");
        }

        await context.SaveChangesAsync();
        return user.Id;
    }

    private static async Task SeedContentAsync(BackendDbContext context, string adminUserId)
    {
        var languageId = await LanguageSeeder.SeedAsync(context);
        var courseIds = await CourseSeeder.SeedAsync(context, languageId);
        var lessonIds = await LessonSeeder.SeedAsync(context, courseIds[0]);
        await ExerciseSeeder.SeedAsync(context, lessonIds);
        await AchievementSeeder.SeedAsync(context);
    }

    private static async Task SeedRolesAsync(
        BackendDbContext context,
        UserManager<User> userManager
    )
    {
        string[] roles = ["Admin", "Student", "Teacher", "ContentCreator"];

        foreach (string currRole in roles)
        {
            var roleStore = new RoleStore<IdentityRole, BackendDbContext>(context);

            if (!context.Roles.Any(role => role.Name == currRole))
            {
                var identityRole = new IdentityRole(currRole)
                {
                    NormalizedName = userManager.NormalizeName(currRole),
                    ConcurrencyStamp = Guid.NewGuid().ToString("D"),
                };

                await roleStore.CreateAsync(identityRole);
            }
        }

        await context.SaveChangesAsync();
    }
}