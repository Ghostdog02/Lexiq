using Lexiq.Api.Dtos;
using Lexiq.Api.Mapping;
using Lexiq.Api.Services;
using Lexiq.Database;
using Lexiq.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lexiq.Api.Controllers
{
    [Route("api/userManagement")]
    [ApiController]
    public class UserManagementController(LexiqDbContext context, UserManager<User> userManager)
        : ControllerBase
    {
        private readonly LexiqDbContext _context = context;
        private readonly UserManager<User> _userManager = userManager;

        // GET: api/userManagement
        [HttpGet]
        public async Task<ActionResult<List<UserDetailsDto>>> GetAsync()
        {
            var users = await _context.Users.MapUsersToDtos().ToListAsync();

            if (users == null || users.Count == 0)
            {
                return NotFound("No users found.");
            }

            return Ok(users);
        }

        // GET api/userManagement/5
        [HttpGet("{id:int}", Name = "GetUserById")]
        public async Task<ActionResult<UserDetailsDto>> GetAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound($"User with ID {id} not found.");
            }

            UserDetailsDto dto = user.MapUserToDto();

            return Ok(dto);
        }

        // GET api/userManagement/example@gmail.com
        [HttpGet("{email}", Name = "GetUserByEmail")]
        public async Task<ActionResult<UserDetailsDto>> GetAsync(string email)
        {
            User? user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return NotFound($"User with ID {email} not found.");
            }

            UserDetailsDto dto = user.MapUserToDto();

            return Ok(dto);
        }

        // POST api/userManagement
        [HttpPost]
        public async Task<ActionResult> PostAsync([FromBody] UserCreationDto newUser)
        {
            if (await _userManager.FindByEmailAsync(newUser.Email) != null)
            {
                return Conflict($"User with email {newUser.Email} already exists.");
            }

            User user = newUser.ToEntity();

            var result = await _userManager.CreateAsync(user);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _context.SaveChangesAsync();

            var readDto = user.MapUserToDto();

            return CreatedAtRoute("GetUserByEmail", new { email = readDto.Email }, value: readDto);
        }

        // POST api/userManagement/assignRole
        [HttpPost("assignRole")]
        public async Task<ActionResult> AssignRoleAsync([FromBody] UserRoleDto userRoleDto)
        {
            User? user = await _userManager.FindByIdAsync(userRoleDto.UserId.ToString());

            if (user == null)
            {
                return NotFound($"User with ID {userRoleDto.UserId} not found.");
            }

            var rolesCount = await _userManager.GetRolesAsync(user);

            if (rolesCount.Count() != 0)
            {
                return NoContent();
            }

            var result = await _userManager.AddToRoleAsync(user, userRoleDto.RoleName);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT api/userManagement/5
        [HttpPut("{id}")]
        public async Task<IResult> Put(int id, [FromBody] UpdatedUserDto dto)
        {
            User? existingUser = await _userManager.FindByIdAsync(id.ToString());

            if (existingUser == null)
            {
                return Results.NotFound($"User with id {id} was not found.");
            }

            existingUser.UpdateUserCredentials(dto, _userManager);

            await _userManager.UpdateAsync(existingUser);

            await _context.SaveChangesAsync();

            return Results.NoContent();
        }

        // PUT api/userManagement/updateLoginDate/5
        [HttpPut("updateLoginDate/{id}", Name = "UpdateLastLoginDateAsync")]
        public async Task<IResult> UpdateLastLoginDateAsync(int id)
        {
            User? existingUser = await _userManager.FindByIdAsync(id.ToString());

            if (existingUser == null)
            {
                return Results.NotFound($"User with id {id} was not found.");
            }

            existingUser.UpdateLastLoginDate();

            await _userManager.UpdateAsync(existingUser);

            await _context.SaveChangesAsync();

            return Results.NoContent();
        }

        // DELETE api/userManagementApi/5
        [HttpDelete("{id}")]
        public async Task<IResult> DeleteAsync(int id)
        {
            User? existingUser = await _userManager.FindByIdAsync(id.ToString());

            if (existingUser == null)
            {
                return Results.NotFound($"User with id {id} was not found.");
            }

            await _userManager.DeleteAsync(existingUser);

            await _context.SaveChangesAsync();

            return Results.NoContent();
        }
    }
}
