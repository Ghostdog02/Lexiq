using System.Security.Claims;
using Backend.Database;
using Backend.Database.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Middleware;

/// <summary>
/// Middleware that extracts the user entity from JWT claims and attaches it to HttpContext.
/// </summary>
public class UserContextMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;
    private const string UserContextKey = "CurrentUser";

    public async Task InvokeAsync(HttpContext context, BackendDbContext dbContext)
    {
        Console.WriteLine(
            $"🔍 UserContextMiddleware: IsAuthenticated = {context.User.Identity?.IsAuthenticated}"
        );

        // Only process if user is authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // Extract user ID from JWT 'sub' claim (mapped to NameIdentifier by ASP.NET Core)
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"🔍 UserContextMiddleware: UserId from JWT = {userId}");

            if (!string.IsNullOrEmpty(userId))
            {
                // Load user entity from database
                var user = await dbContext
                    .Users.Include(u => u.UserLanguages)
                    .ThenInclude(ul => ul.Language)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                Console.WriteLine($"🔍 UserContextMiddleware: User found in DB = {user != null}");

                if (user != null)
                {
                    // Attach user to HttpContext for controller access
                    context.Items[UserContextKey] = user;
                    Console.WriteLine(
                        $"✅ UserContextMiddleware: User {user.Email} attached to context"
                    );
                }
                else
                {
                    Console.WriteLine(
                        $"❌ UserContextMiddleware: User with ID {userId} not found in database"
                    );
                }
            }
            else
            {
                Console.WriteLine($"❌ UserContextMiddleware: No userId in JWT claims");
            }
        }
        else
        {
            Console.WriteLine($"❌ UserContextMiddleware: User is not authenticated");
        }

        await _next(context);
    }
}

/// <summary>
/// Extension method to easily retrieve the current user from HttpContext.
/// </summary>
public static class HttpContextExtensions
{
    public static User? GetCurrentUser(this HttpContext context)
    {
        return context.Items.TryGetValue("CurrentUser", out var user) ? user as User : null;
    }
}
