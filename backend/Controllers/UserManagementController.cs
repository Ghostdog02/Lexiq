using Backend.Api.Dtos;
using Backend.Api.Mapping;
using Backend.Api.Services;
using Backend.Database;
using Backend.Database.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserManagementController(BackendDbContext context, UserManager<User> userManager)
    : ControllerBase
{
    private readonly BackendDbContext _context = context;
    private readonly UserManager<User> _userManager = userManager;

    [HttpGet]
    public async Task<ActionResult<List<UserDetailsDto>>> GetAll()
    {
        var users = await _context.Users.MapUsersToDtos().ToListAsync();

        if (users.Count == 0)
            return NotFound("No users found.");

        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDetailsDto>> GetById(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
            return NotFound($"User with ID {id} not found.");

        return Ok(user.MapUserToDto());
    }

    [HttpGet("email/{email}")]
    public async Task<ActionResult<UserDetailsDto>> GetByEmail(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
            return NotFound($"User with email {email} not found.");

        return Ok(user.MapUserToDto());
    }

    [HttpPost("assignRole")]
    public async Task<IActionResult> AssignRole([FromBody] UserRoleDto userRoleDto)
    {
        var user = await _userManager.FindByIdAsync(userRoleDto.UserId.ToString());

        if (user == null)
            return NotFound($"User with ID {userRoleDto.UserId} not found.");

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Count != 0)
            return NoContent();

        var result = await _userManager.AddToRoleAsync(user, userRoleDto.RoleName);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserDto dto)
    {
        var existingUser = await _userManager.FindByIdAsync(id);

        if (existingUser == null)
            return NotFound($"User with id {id} was not found.");

        existingUser.UpdateUserCredentials(dto, _userManager);
        await _userManager.UpdateAsync(existingUser);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("updateLoginDate/{id}")]
    public async Task<IActionResult> UpdateLastLoginDate(string id)
    {
        var existingUser = await _userManager.FindByIdAsync(id);

        if (existingUser == null)
            return NotFound($"User with id {id} was not found.");

        existingUser.UpdateLastLoginDate();
        await _userManager.UpdateAsync(existingUser);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var existingUser = await _userManager.FindByIdAsync(id);

        if (existingUser == null)
            return NotFound($"User with id {id} was not found.");

        await _userManager.DeleteAsync(existingUser);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
