using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Backend.Api.Dtos;
using Backend.Api.Mapping;
using Backend.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CourseController(CourseService courseService) : ControllerBase
{
    private readonly CourseService _courseService = courseService;

    [HttpGet]
    public async Task<ActionResult<List<CourseDto>>> GetCourses()
    {
        var courses = await _courseService.GetAllCoursesAsync();
        return Ok(courses.Select(c => c.ToDto()));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CourseDto>> GetCourse(string id)
    {
        var course = await _courseService.GetCourseByIdAsync(id);

        if (course == null)
            return NotFound();

        return Ok(course.ToDto());
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ContentCreator")]
    public async Task<ActionResult<CourseDto>> CreateCourse(CreateCourseDto dto)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (userId == null)
            return Unauthorized();

        var course = await _courseService.CreateCourseAsync(dto, userId);
        return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, course.ToDto());
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,ContentCreator")]
    public async Task<ActionResult<CourseDto>> UpdateCourse(string id, UpdateCourseDto dto)
    {
        var course = await _courseService.UpdateCourseAsync(id, dto);
        if (course == null)
            return NotFound();

        return Ok(course.ToDto());
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCourse(string id)
    {
        var result = await _courseService.DeleteCourseAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }
}