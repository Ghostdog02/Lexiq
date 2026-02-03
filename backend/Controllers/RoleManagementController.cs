using Backend.Database;
using Backend.Database.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RoleManagementController(BackendDbContext context, UserManager<User> userManager)
    : ControllerBase
{
    private readonly BackendDbContext _context = context;
    private readonly UserManager<User> _userManager = userManager;

    [HttpGet("{email}")]
    public async Task<ActionResult<string>> GetRoleByEmail([FromRoute] string email)
    {
        User user =
            await _userManager.FindByEmailAsync(email)
            ?? throw new ArgumentException(
                $"The user with the given email {email} has not been found"
            );

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.First();

        return role;
    }
}
