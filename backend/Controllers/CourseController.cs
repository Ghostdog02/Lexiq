using Backend.Api.Dtos;
using Backend.Api.Mapping;
using Backend.Api.Middleware;
using Backend.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[Route("api/[controller]s")]
[ApiController]
[Authorize]
public class CourseController(
    CourseService courseService,
    LessonService lessonService,
    LessonProgressService lessonProgressService,
    HeartsService heartsService
) : ControllerBase
{
    private readonly CourseService _courseService = courseService;
    private readonly LessonService _lessonService = lessonService;
    private readonly LessonProgressService _lessonProgressService = lessonProgressService;
    private readonly HeartsService _heartsService = heartsService;

    [HttpGet("with-progress")]
    public async Task<ActionResult<CoursesWithProgressDto>> GetCoursesWithProgress()
    {
        var currentUser = HttpContext.GetCurrentUser()!;
        var userId = currentUser.Id;

        var courses = await _courseService.GetAllCoursesAsync();

        var lessonsByCourse = new Dictionary<string, List<Database.Entities.Lesson>>();
        foreach (var course in courses)
        {
            await _lessonProgressService.EnsureFirstLessonUnlockedAsync(userId, course.CourseId);
            var lessons = await _lessonService.GetLessonsByCourseAsync(course.CourseId);
            if (lessons != null)
                lessonsByCourse[course.CourseId] = lessons;
        }

        var allLessonIds = lessonsByCourse.Values
            .SelectMany(l => l)
            .Select(l => l.LessonId)
            .ToList();

        var (progressMap, unlockedIds) = await _lessonProgressService.GetProgressForLessonsAsync(
            userId,
            allLessonIds
        );

        var hearts = await _heartsService.RefillAndGetHeartsAsync(currentUser);
        DateTime? nextHeartRefillAt = hearts < HeartsService.MaxHearts
            ? currentUser.LastHeartResetAt.AddHours(HeartsService.RefillIntervalHours)
            : null;

        var courseDtos = courses.Select(course =>
        {
            var lessons = lessonsByCourse.GetValueOrDefault(course.CourseId) ?? [];
            var lessonDtos = lessons.Select(l => l.ToHomeLessonDto(
                course.Title,
                progressMap.GetValueOrDefault(l.LessonId),
                !unlockedIds.Contains(l.LessonId)
            )).ToList();
            return course.ToHomeCourseDto(lessonDtos);
        }).ToList();

        return Ok(new CoursesWithProgressDto(
            Courses: courseDtos,
            TotalXp: currentUser.TotalPointsEarned,
            Hearts: hearts,
            NextHeartRefillAt: nextHeartRefillAt
        ));
    }

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
        var user = HttpContext.GetCurrentUser();
        if (user == null)
            return Unauthorized();

        var course = await _courseService.CreateCourseAsync(dto);
        return CreatedAtAction(nameof(GetCourse), new { id = course.CourseId }, course.ToDto());
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
