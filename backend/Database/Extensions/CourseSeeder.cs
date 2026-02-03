using Backend.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database.Extensions;

/// <summary>
/// Seeds the "Italian for Beginners" course into the database.
/// Returns the course ID for use by downstream seeders.
/// </summary>
public static class CourseSeeder
{
    private const string CourseTitle = "Italian for Beginners";

    public static async Task<string> SeedAsync(
        BackendDbContext context,
        string languageId,
        string adminUserId
    )
    {
        var existing = await context.Courses.FirstOrDefaultAsync(c =>
            c.LanguageId == languageId && c.Title == CourseTitle
        );

        if (existing != null)
        {
            return existing.Id;
        }

        var course = new Course
        {
            LanguageId = languageId,
            Title = CourseTitle,
            Description =
                "A structured introduction to Italian covering greetings, "
                + "essential vocabulary, present tense verbs, and everyday conversation. "
                + "Designed for Bulgarian speakers learning Italian from scratch.",
            EstimatedDurationHours = 12,
            OrderIndex = 0,
            CreatedById = adminUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        context.Courses.Add(course);
        await context.SaveChangesAsync();

        return course.Id;
    }
}
