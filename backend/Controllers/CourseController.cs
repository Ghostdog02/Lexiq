using System.Security.Claims;
using Backend.Api.Dtos;
using Backend.Api.Mapping;
using Backend.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[Route("api/courses")]
[ApiController]
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
    public async Task<ActionResult<CourseDto>> GetCourse(int id)
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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        var course = await _courseService.CreateCourseAsync(dto, userId);
        return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, course.ToDto());
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,ContentCreator")]
    public async Task<ActionResult<CourseDto>> UpdateCourse(int id, UpdateCourseDto dto)
    {
        var course = await _courseService.UpdateCourseAsync(id, dto);
        if (course == null)
            return NotFound();

        return Ok(course.ToDto());
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var result = await _courseService.DeleteCourseAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }
}