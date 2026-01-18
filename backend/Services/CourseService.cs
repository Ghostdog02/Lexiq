using Backend.Database;
using Backend.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    public class CourseService(BackendDbContext context)
    {
        private readonly BackendDbContext _context = context;

        public async Task<List<Course>> GetAllCoursesAsync()
        {
            return await _context.Courses.Include(c => c.Language).ToListAsync();
        }

        public async Task<Course?> GetCourseByIdAsync(int id)
        {
            return await _context.Courses
                .Include(c => c.Language)
                .Include(c => c.Lessons)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Course> CreateCourseAsync(Backend.Api.Dtos.CreateCourseDto dto, string createdById)
        {
            var course = new Course
            {
                LanguageId = dto.LanguageId,
                Title = dto.Title,
                Description = dto.Description,
                EstimatedDurationHours = dto.EstimatedDurationHours,
                OrderIndex = dto.OrderIndex,
                CreatedById = createdById,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
            return course;
        }

        public async Task<Course?> UpdateCourseAsync(int id, Backend.Api.Dtos.UpdateCourseDto dto)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
                return null;

            if (dto.Title != null) course.Title = dto.Title;
            if (dto.Description != null) course.Description = dto.Description;
            if (dto.EstimatedDurationHours.HasValue) course.EstimatedDurationHours = dto.EstimatedDurationHours.Value;
            if (dto.OrderIndex.HasValue) course.OrderIndex = dto.OrderIndex.Value;
            
            course.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return course;
        }

        public async Task<bool> DeleteCourseAsync(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
                return false;

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}