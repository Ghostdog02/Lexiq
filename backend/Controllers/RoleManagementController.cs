using Backend.Database.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RoleManagementController(UserManager<User> userManager) : ControllerBase
{
    private readonly UserManager<User> _userManager = userManager;

    [HttpGet("{email}")]
    public async Task<ActionResult<string>> GetRoleByEmail([FromRoute] string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return NotFound($"User with email {email} not found.");

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault();
        if (role == null)
            return NotFound($"No role assigned to user {email}.");

        return Ok(role);
    }
}
