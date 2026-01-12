using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers
{
    [Route("api/courses")]
    [ApiController]
    public class Courses : ControllerBase
    {
        [HttpGet]
        public IActionResult GetCourses()
        {
            
            var courses = new List<string> { "Course1", "Course2", "Course3" };
            return Ok(courses);
        }
    }
}
