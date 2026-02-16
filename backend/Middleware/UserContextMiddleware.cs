using System.Security.Claims;
using Backend.Database;
using Backend.Database.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Middleware;

public class UserContextMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;
    private const string UserContextKey = "CurrentUser";

    public async Task InvokeAsync(HttpContext context, BackendDbContext dbContext)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrEmpty(userId))
        {
            var user = await dbContext
                .Users.Include(u => u.UserLanguages)
                .ThenInclude(ul => ul.Language)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user != null)
                context.Items[UserContextKey] = user;
        }

        await _next(context);
    }
}

public static class HttpContextExtensions
{
    public static User? GetCurrentUser(this HttpContext context)
    {
        return context.Items.TryGetValue("CurrentUser", out var user) ? user as User : null;
    }
}
