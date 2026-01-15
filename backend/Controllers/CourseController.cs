using Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers
{
    [Route("api/courses")]
    [ApiController]
    public class CourseController(CourseService courseService) : ControllerBase
    {
        private readonly CourseService _courseService = courseService;

        [HttpGet]
        public async Task<IActionResult> GetCourses()
        {
            var courses = await _courseService.GetAllCoursesAsync();
            return Ok(courses);
        }
    }
}
